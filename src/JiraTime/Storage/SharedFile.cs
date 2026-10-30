namespace JiraTime.Storage;

/// <summary>
/// File reads and replaces that live with other programs using the same files. On Windows a file another program
/// has open can't be replaced or deleted for a moment (an editor, antivirus, a backup, or the store reading it).
/// </summary>
internal static class SharedFile
{
    private const int Tries = 6;

    /// <summary>Reads a file without stopping anyone else from writing, replacing or deleting it meanwhile.</summary>
    public static async Task<string> ReadAllTextAsync(string path, CancellationToken ct)
    {
        await using var stream = new FileStream(path, new FileStreamOptions
        {
            Access = FileAccess.Read,
            Share = FileShare.ReadWrite | FileShare.Delete,
            Options = FileOptions.Asynchronous,
        });
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(ct);
    }

    /// <summary>Moves <paramref name="source"/> over <paramref name="destination"/>, waiting briefly while the file is in use.</summary>
    public static Task ReplaceAsync(string source, string destination, CancellationToken ct = default) =>
        RetryWhileInUseAsync(() => File.Move(source, destination, overwrite: true), ct);

    /// <summary>Deletes a file, waiting briefly while it is in use.</summary>
    public static Task DeleteAsync(string path, CancellationToken ct = default) =>
        RetryWhileInUseAsync(() => File.Delete(path), ct);

    private static async Task RetryWhileInUseAsync(Action change, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                change();
                return;
            }
            catch (Exception e) when (attempt < Tries && e is UnauthorizedAccessException or IOException and not FileNotFoundException and not DirectoryNotFoundException)
            {
                // 10, 20, 40, 80, 160 ms: about a third of a second in all before giving up.
                await Task.Delay(TimeSpan.FromMilliseconds(10 << (attempt - 1)), ct);
            }
        }
    }
}
