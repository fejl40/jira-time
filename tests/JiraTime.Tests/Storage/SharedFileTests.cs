using JiraTime.Storage;

namespace JiraTime.Tests.Storage;

/// <summary>Reading a document never stops another program from changing it (this matters on Windows).</summary>
public class SharedFileTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("jira-time-shared-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Write(string name, string text)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, text);
        return path;
    }

    [Fact]
    public async Task A_file_open_for_a_moment_is_replaced_once_it_is_closed()
    {
        var path = Write("doc.json", "old");
        var tmp = Write("doc.json.tmp", "new");
        var reading = new FileStream(path, new FileStreamOptions
        {
            Access = FileAccess.Read,
            Share = FileShare.ReadWrite | FileShare.Delete,
        });

        // Windows refuses to replace a file while it is open; the replace waits until the reader is done.
        var replacing = SharedFile.ReplaceAsync(tmp, path);
        await Task.Delay(30);
        await reading.DisposeAsync();
        await replacing;

        Assert.Equal("new", await SharedFile.ReadAllTextAsync(path, default));
    }

    [Fact]
    public async Task A_file_being_read_can_be_deleted()
    {
        var path = Write("doc.json", "text");
        await using (new FileStream(path, new FileStreamOptions { Access = FileAccess.Read, Share = FileShare.ReadWrite | FileShare.Delete }))
            await SharedFile.DeleteAsync(path);

        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task Reading_lets_others_write_meanwhile()
    {
        var path = Write("doc.json", "text");

        Assert.Equal("text", await SharedFile.ReadAllTextAsync(path, default));
        await File.WriteAllTextAsync(path, "changed");
        Assert.Equal("changed", await SharedFile.ReadAllTextAsync(path, default));
    }

    [Fact]
    public async Task A_missing_file_fails_at_once_without_waiting()
    {
        var missing = Path.Combine(_dir, "missing.json");

        await Assert.ThrowsAsync<FileNotFoundException>(() => SharedFile.ReplaceAsync(missing, Path.Combine(_dir, "doc.json")));
        await Assert.ThrowsAsync<FileNotFoundException>(() => SharedFile.ReadAllTextAsync(missing, default));
    }
}
