using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace JiraTime.Tests;

/// <summary>
/// The app's services wired to a fake Jira and a temporary store, signed in as Carl:
/// Jira is the source of truth, the store holds the retry queue, days off and settings.
/// </summary>
public sealed class TimeHarness : IDisposable
{
    public static readonly JiraConnection Server = new("https://jira.example.com", JiraAuthType.Pat, "token", null);
    public static readonly JiraConnection Cloud = new("https://acme.atlassian.net", JiraAuthType.Cloud, "token", "carl@example.com");

    public TempStore Files { get; } = new();
    public FileDocumentStore Store => Files.Store;
    public FakeJira Jira { get; } = new();
    public TestClock Clock { get; } = new();
    public RetryQueue Queue { get; }
    public RecentWorklogIssues Recent { get; }
    public TimeLog Log { get; }
    public WorklogSync Sync { get; }
    public JiraConnection Connection { get; }
    public CurrentUser User { get; }
    public JiraIssueLookup Issues { get; }
    public WorkCalendarService Calendar { get; }
    public LegacyDataMigration Legacy { get; }
    public JiraRequestLog RequestLog { get; }
    public JiraRequestFeed Feed { get; } = new(NullLogger<JiraRequestFeed>.Instance);
    public JiraRequestRecorder Recorder { get; }

    public TimeHarness(JiraConnection? connection = null)
    {
        var user = User = CurrentUser.SignedIn(TestUsers.Carl, connection: connection ?? Server);
        Connection = user.Connection!;
        RequestLog = new JiraRequestLog(System.IO.Path.Combine(Files.Path, "jira-logs"), Clock);
        Recorder = new JiraRequestRecorder(RequestLog, Feed, Clock, NullLogger<JiraRequestRecorder>.Instance);
        Issues = new JiraIssueLookup(new JiraClient(Jira.Client(), Recorder), Clock);
        Calendar = new WorkCalendarService(Store, user);
        Legacy = new LegacyDataMigration(Store, Clock);
        var client = new JiraWorklogClient(Jira.Client(), Recorder);
        Queue = new RetryQueue(Store, user);
        Recent = new RecentWorklogIssues(Clock);
        Log = new TimeLog(client, Queue, user, Recent, Clock);
        Sync = new WorklogSync(client, Queue, Log, Clock);
    }

    public DateOnly Today => DateOnly.FromDateTime(Clock.Now.UtcDateTime);

    /// <summary>Another user's stored data, over the same store.</summary>
    public UserData For(UserKey key)
    {
        var user = CurrentUser.SignedIn(key, connection: Connection);
        return new UserData(new RetryQueue(Store, user), new WorkCalendarService(Store, user), new EpicExportStore(Store, user));
    }

    /// <summary>Signing in through the fake Jira; <paramref name="tab"/> becomes the user Jira confirms.</summary>
    public JiraIdentity Identity(CurrentUser tab) => new(new JiraClient(Jira.Client(), Recorder), Store, tab, Legacy, Clock);

    /// <summary>A period loader and status book over the same fake Jira and store.</summary>
    public (PeriodLoader Loader, IssueStatuses Statuses) PeriodLoading() =>
        (new PeriodLoader(Log, Calendar, Issues, User), new IssueStatuses(Issues, User));

    /// <summary>A month export loader and the epic values store over the same fake Jira and store.</summary>
    public (MonthExportLoader Loader, EpicExportStore Values) ExportLoading()
    {
        var values = new EpicExportStore(Store, User);
        return (new MonthExportLoader(Log, Issues, values, User), values);
    }

    /// <summary>The day's work as the timesheet reads it.</summary>
    public async Task<IReadOnlyList<TimeEntry>> DayAsync(DateOnly day) => (await Log.GetRangeAsync(day, day)).Entries;

    /// <summary>Saves the day the way the timesheet does: rows edited from what was read.</summary>
    public async Task<WorklogSync.DayResult> SaveDayAsync(DateOnly day, IReadOnlyList<TimeEntry> rows, IReadOnlyList<TimeEntry>? saved = null) =>
        await Sync.SaveDayAsync(Connection, day, rows, saved ?? await DayAsync(day));

    /// <summary>Editable copies of the day's work.</summary>
    public async Task<List<TimeEntry>> CopiesAsync(DateOnly day) => (await DayAsync(day)).Select(e => e.Copy()).ToList();

    /// <summary>Files in the retry queue, for any day.</summary>
    public Task<IReadOnlyList<TimeEntry>> QueuedAsync() => Queue.GetPendingAsync(DateOnly.MinValue, DateOnly.MaxValue);

    public int JiraSeconds(string key) =>
        Jira.Worklogs.TryGetValue(key, out var logs) ? logs.Values.Sum(l => (int)l["timeSpentSeconds"]!) : 0;

    public static TimeEntry Row(string key, double hours, string? comment = null) =>
        new() { IssueKey = key, IssueSummary = $"Summary of {key}", Duration = TimeSpan.FromHours(hours), Description = comment };

    public void Dispose() => Files.Dispose();
}

/// <summary>One user's stored data: the retry queue, days and work hours, and export values per epic.</summary>
public sealed record UserData(RetryQueue Queue, WorkCalendarService Calendar, EpicExportStore ExportValues);

/// <summary>Two users on <see cref="TimeHarness.Server"/>.</summary>
public static class TestUsers
{
    public static readonly UserKey Carl = UserKey.For(TimeHarness.Server.BaseUrl, "JIRAUSER1");
    public static readonly UserKey Anna = UserKey.For(TimeHarness.Server.BaseUrl, "JIRAUSER2");

    public static CurrentUser SignedIn(UserKey? key = null) => CurrentUser.SignedIn(key ?? Carl);
}

/// <summary>A clock tests can set and move. Without a time it is noon UTC on Tuesday 27 October 2026.</summary>
public sealed class TestClock(DateTimeOffset now) : TimeProvider
{
    public TestClock() : this(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero)) { }

    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    public void Advance(TimeSpan by) => Now += by;
}

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
