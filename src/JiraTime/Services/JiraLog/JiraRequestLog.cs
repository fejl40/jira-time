using System.Globalization;
using System.Text;
using System.Text.Json;
using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>
/// The Jira request log on disk: one JSON line per request in <c>&lt;folder&gt;/&lt;owner&gt;/yyyy-MM-dd.jsonl</c>,
/// one file per user per day. Files older than <see cref="KeepFor"/> are deleted.
/// </summary>
public sealed class JiraRequestLog(string folder, TimeProvider clock)
{
    public static readonly TimeSpan KeepFor = TimeSpan.FromDays(30);

    /// <summary>Folder for requests no signed-in user can be named for, such as a token Jira rejected.</summary>
    public const string SignedOut = "signed-out";

    private const string DayFormat = "yyyy-MM-dd";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // One writer at a time keeps lines whole when requests finish in parallel.
    private readonly SemaphoreSlim _lock = new(1, 1);
    private DateOnly _cleanedOn;

    /// <summary>Appends the entry to its owner's file for the day it was made.</summary>
    public async Task AppendAsync(JiraLogEntry entry)
    {
        var line = JsonSerializer.Serialize(entry, Json) + "\n";
        var file = FileFor(entry.Owner, DayOf(entry.Time));
        await _lock.WaitAsync();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            await File.AppendAllTextAsync(file, line, Encoding.UTF8);
            DeleteOldOncePerDay();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Days the user has a log for, newest first.</summary>
    public IReadOnlyList<DateOnly> Days(string owner)
    {
        var dir = OwnerFolder(owner);
        if (!Directory.Exists(dir)) return [];
        var days = new List<DateOnly>();
        foreach (var file in Directory.EnumerateFiles(dir, "*.jsonl"))
            if (ParseDay(file) is { } day) days.Add(day);
        days.Sort((a, b) => b.CompareTo(a));
        return days;
    }

    /// <summary>The user's requests on a day, in the order they finished. A line that can't be read is skipped.</summary>
    public async Task<IReadOnlyList<JiraLogEntry>> ReadDayAsync(string owner, DateOnly day)
    {
        var entries = new List<JiraLogEntry>();
        foreach (var line in (await ReadDayTextAsync(owner, day)).Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                if (JsonSerializer.Deserialize<JiraLogEntry>(line, Json) is { } entry) entries.Add(entry);
            }
            catch (JsonException) { }
        }
        return entries;
    }

    /// <summary>The day's file as it is on disk (empty when there is none), for downloading.</summary>
    public async Task<string> ReadDayTextAsync(string owner, DateOnly day)
    {
        var file = FileFor(owner, day);
        if (!File.Exists(file)) return "";
        // Shared with the writer, so a read never waits for or blocks a request being logged.
        await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    /// <summary>Deletes all of the user's log files.</summary>
    public async Task ClearAsync(string owner)
    {
        await _lock.WaitAsync();
        try
        {
            var dir = OwnerFolder(owner);
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>The day a request belongs to: its date in the server's time zone.</summary>
    public DateOnly DayOf(DateTimeOffset time) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time, clock.LocalTimeZone).DateTime);

    public string FileFor(string owner, DateOnly day) =>
        Path.Combine(OwnerFolder(owner), day.ToString(DayFormat, CultureInfo.InvariantCulture) + ".jsonl");

    private string OwnerFolder(string owner)
    {
        // Owner ids are hashes; anything that could leave the folder is a bug.
        if (owner == "" || Path.GetFileName(owner) != owner || owner.Contains(".."))
            throw new ArgumentException($"Not an owner id: {owner}", nameof(owner));
        return Path.Combine(folder, owner);
    }

    private void DeleteOldOncePerDay()
    {
        var today = DayOf(clock.GetUtcNow());
        if (_cleanedOn == today || !Directory.Exists(folder)) return;
        _cleanedOn = today;
        var oldest = today.AddDays(-KeepFor.Days);
        foreach (var file in Directory.EnumerateFiles(folder, "*.jsonl", SearchOption.AllDirectories))
        {
            if (ParseDay(file) is { } day && day < oldest)
                File.Delete(file);
        }
    }

    private static DateOnly? ParseDay(string file) =>
        DateOnly.TryParseExact(Path.GetFileNameWithoutExtension(file), DayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
            ? day : null;
}
