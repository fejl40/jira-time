using System.Linq.Expressions;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JiraTime.Storage;

internal sealed partial class FileDocumentCollection<T> : IDocumentCollection<T>, IDisposable
    where T : class, IDocument
{
    private readonly string _dir;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private FileSystemWatcher? _watcher;

    /// <summary>
    /// id → raw JSON. Raw so callers always get their own copy to mutate. A published dictionary is never
    /// changed: readers enumerate it without the lock, so writers swap in a changed copy instead.
    /// The file watcher sets it to null from its own thread at any moment.
    /// </summary>
    private Dictionary<string, string>? _cache;

    /// <summary>Counts changes the watcher saw, so a load that raced one isn't kept.</summary>
    private int _changesOnDisk;

    public FileDocumentCollection(string dir)
    {
        _dir = dir;
        Directory.CreateDirectory(dir);
    }

    /// <summary>
    /// Drops the cache when files change on disk (hand edits, restores from backup, ...). Started on the
    /// first read, since only a cache needs it: each watcher holds an OS handle (an inotify instance on Linux).
    /// </summary>
    private FileSystemWatcher Watch()
    {
        var watcher = new FileSystemWatcher(_dir, "*.json") { EnableRaisingEvents = true };
        watcher.Changed += (_, _) => Forget();
        watcher.Created += (_, _) => Forget();
        watcher.Deleted += (_, _) => Forget();
        watcher.Renamed += (_, _) => Forget();
        return watcher;
    }

    private void Forget()
    {
        Interlocked.Increment(ref _changesOnDisk);
        Volatile.Write(ref _cache, null);
    }

    public async Task<T?> GetAsync(string id, CancellationToken ct = default)
    {
        var docs = await LoadAsync(ct);
        return docs.TryGetValue(id, out var json) ? Deserialize(json) : null;
    }

    public async Task<IReadOnlyList<T>> QueryAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        var docs = await LoadAsync(ct);
        var match = predicate.Compile();
        return docs.Values.Select(Deserialize).Where(match).ToList();
    }

    public async Task UpsertAsync(T document, CancellationToken ct = default)
    {
        ValidateId(document.Id);
        var json = JsonSerializer.Serialize(document, FileDocumentStore.Json);

        await _lock.WaitAsync(ct);
        try
        {
            // Write to a temp file and move it into place so a crash never leaves half a document.
            var path = PathFor(document.Id);
            var tmp = path + ".tmp";
            await File.WriteAllTextAsync(tmp, json, ct);
            File.Move(tmp, path, overwrite: true);
            Remember(cache => cache[document.Id] = json);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        ValidateId(id);
        await _lock.WaitAsync(ct);
        try
        {
            var path = PathFor(id);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            Remember(cache => cache.Remove(id));
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<Dictionary<string, string>> LoadAsync(CancellationToken ct)
    {
        if (Volatile.Read(ref _cache) is { } cached) return cached;

        await _lock.WaitAsync(ct);
        try
        {
            if (Volatile.Read(ref _cache) is { } c) return c;
            _watcher ??= Watch();
            var changesBefore = Volatile.Read(ref _changesOnDisk);
            var docs = new Dictionary<string, string>();
            foreach (var file in Directory.EnumerateFiles(_dir, "*.json"))
                docs[Path.GetFileNameWithoutExtension(file)] = await File.ReadAllTextAsync(file, ct);
            // A file that changed while we read may be missing or old here: use it once, don't keep it.
            if (Volatile.Read(ref _changesOnDisk) == changesBefore) Interlocked.CompareExchange(ref _cache, docs, null);
            return docs;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Applies a write to a copy of the cache and publishes it, unless the watcher dropped the cache
    /// meanwhile: then it stays dropped and the next read loads from disk. Call while holding the lock.
    /// </summary>
    private void Remember(Action<Dictionary<string, string>> write)
    {
        if (Volatile.Read(ref _cache) is not { } cached) return;
        var changed = new Dictionary<string, string>(cached);
        write(changed);
        Interlocked.CompareExchange(ref _cache, changed, cached);
    }

    private static T Deserialize(string json) =>
        JsonSerializer.Deserialize<T>(json, FileDocumentStore.Json)
        ?? throw new InvalidDataException($"Empty {typeof(T).Name} document");

    private string PathFor(string id) => Path.Combine(_dir, id + ".json");

    /// <summary>Ids become file names, so keep them to a safe character set.</summary>
    private static void ValidateId(string id)
    {
        if (!SafeId().IsMatch(id))
            throw new ArgumentException($"Invalid document id '{id}'. Use letters, digits, '-' and '_' only.", nameof(id));
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{1,128}$")]
    private static partial Regex SafeId();

    public void Dispose()
    {
        _watcher?.Dispose();
        _lock.Dispose();
    }
}
