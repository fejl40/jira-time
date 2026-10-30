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
    public async Task A_file_being_read_can_be_replaced_and_deleted()
    {
        var path = Write("doc.json", "old");
        await using var reading = new FileStream(path, new FileStreamOptions
        {
            Access = FileAccess.Read,
            Share = FileShare.ReadWrite | FileShare.Delete,
        });

        await SharedFile.ReplaceAsync(Write("doc.json.tmp", "new"), path);
        Assert.Equal("new", await SharedFile.ReadAllTextAsync(path, default));
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
    }
}
