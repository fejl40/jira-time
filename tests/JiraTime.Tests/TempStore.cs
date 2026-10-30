using JiraTime.Storage;

namespace JiraTime.Tests;

/// <summary>A file document store in its own temp folder, deleted after the test.</summary>
public sealed class TempStore : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "jiratime-tests-" + Guid.NewGuid().ToString("N"));
    public FileDocumentStore Store { get; private set; }

    public TempStore() => Store = new FileDocumentStore(Path);

    /// <summary>The folder of one collection, for writing files the way older versions did.</summary>
    public string Folder(string collection) => Directory.CreateDirectory(System.IO.Path.Combine(Path, collection)).FullName;

    /// <summary>Closes the store and opens a new one on the same folder, as after a restart.</summary>
    public FileDocumentStore Reopen()
    {
        Store.Dispose();
        return Store = new FileDocumentStore(Path);
    }

    public void Dispose()
    {
        Store.Dispose();
        try { Directory.Delete(Path, recursive: true); } catch (IOException) { }
    }
}
