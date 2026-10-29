using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Storage;

namespace JiraTime.Tests;

/// <summary>
/// The services that read and write logged time, wired to a fake Jira and a temporary store:
/// Jira is the source of truth, the store holds only the retry queue.
/// </summary>
public sealed class TimeHarness : IDisposable
{
    public static readonly JiraConnection Server = new("https://jira.example.com", JiraAuthType.Pat, "token", null);
    public static readonly JiraConnection Cloud = new("https://acme.atlassian.net", JiraAuthType.Cloud, "token", "carl@example.com");

    private readonly TempDir _dir = new();
    public FileDocumentStore Store { get; }
    public FakeJira Jira { get; } = new();
    public TestClock Clock { get; } = new(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero));
    public RetryQueue Queue { get; }
    public RecentWorklogIssues Recent { get; }
    public TimeLog Log { get; }
    public WorklogSync Sync { get; }
    public JiraConnection Connection { get; }
    public CurrentUser User { get; }
    public JiraIssueLookup Issues { get; }
    public WorkCalendarService Calendar { get; }

    public TimeHarness(JiraConnection? connection = null)
    {
        Connection = connection ?? Server;
        Store = new FileDocumentStore(_dir.Path);
        var user = User = CurrentUser.SignedIn(TestUsers.Carl, connection: Connection);
        Issues = new JiraIssueLookup(new JiraClient(Jira.Client()), Clock);
        Calendar = new WorkCalendarService(Store, user);
        var client = new JiraWorklogClient(Jira.Client());
        Queue = new RetryQueue(Store, user);
        Recent = new RecentWorklogIssues(Clock);
        Log = new TimeLog(client, Queue, user, Recent, Clock);
        Sync = new WorklogSync(client, Queue, Log, Clock);
    }

    public DateOnly Today => DateOnly.FromDateTime(Clock.Now.UtcDateTime);

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

    public void Dispose()
    {
        Store.Dispose();
        _dir.Dispose();
    }
}
