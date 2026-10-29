using System.Linq.Expressions;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JiraTime.Storage;

internal sealed partial class FileDocumentCollection<T> : IDocumentCollection<T>, IDisposable
    where T : class, IDocument
{
    private readonly string _dir;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly FileSystemWatcher _watcher;

    /// <summary>id → raw JSON. Raw so callers always get their own copy to mutate.</summary>
    private Dictionary<string, string>? _cache;

    public FileDocumentCollection(string dir)
    {
        _dir = dir;
        Directory.CreateDirectory(dir);

        // Drop the cache if files change on disk (hand edits, restores from backup, ...).
        _watcher = new FileSystemWatcher(dir, "*.json") { EnableRaisingEvents = true };
        FileSystemEventHandler reset = (_, _) => _cache = null;
        _watcher.Changed += reset;
        _watcher.Created += reset;
        _watcher.Deleted += reset;
        _watcher.Renamed += (_, _) => _cache = null;
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
            if (_cache is not null) _cache[document.Id] = json;
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
            _cache?.Remove(id);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<Dictionary<string, string>> LoadAsync(CancellationToken ct)
    {
        if (_cache is { } cached) return cached;

        await _lock.WaitAsync(ct);
        try
        {
            if (_cache is { } c) return c;
            var docs = new Dictionary<string, string>();
            foreach (var file in Directory.EnumerateFiles(_dir, "*.json"))
                docs[Path.GetFileNameWithoutExtension(file)] = await File.ReadAllTextAsync(file, ct);
            return _cache = docs;
        }
        finally
        {
            _lock.Release();
        }
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
        _watcher.Dispose();
        _lock.Dispose();
    }
}
