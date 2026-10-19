using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JiraTime.Storage.FileSystem;

public class FileDocumentStoreOptions
{
    /// <summary>Root folder. Each collection is a sub-folder, each document a JSON file.</summary>
    public string RootPath { get; set; } = "data/documents";
}

/// <summary>
/// Stores documents as <c>{RootPath}/{collection}/{id}.json</c>. Meant for a single
/// local user: collections are read into memory on first use and written through
/// on every change. Editing files by hand while the app runs is picked up too.
/// </summary>
public sealed class FileDocumentStore : IDocumentStore, IDisposable
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _root;
    private readonly ConcurrentDictionary<Type, IDisposable> _collections = new();

    public FileDocumentStore(FileDocumentStoreOptions options)
    {
        _root = Path.GetFullPath(options.RootPath);
        Directory.CreateDirectory(_root);
    }

    public IDocumentCollection<T> Collection<T>() where T : class, IDocument =>
        (IDocumentCollection<T>)_collections.GetOrAdd(typeof(T), _ =>
        {
            var name = typeof(T).GetCustomAttribute<DocumentCollectionAttribute>()?.Name ?? typeof(T).Name;
            return new FileDocumentCollection<T>(Path.Combine(_root, name));
        });

    public void Dispose()
    {
        foreach (var c in _collections.Values) c.Dispose();
    }
}
