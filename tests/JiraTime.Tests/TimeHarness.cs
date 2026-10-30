using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Storage;
using Microsoft.Extensions.DependencyInjection;
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
    /// <summary>The Jira client as the app registers it (resilience pipeline and request log), over <see cref="Jira"/>.</summary>
    public JiraHttp Http { get; }
    private readonly List<ServiceProvider> _providers = [];

    public TimeHarness(JiraConnection? connection = null)
    {
        var user = User = TestUsers.SignedIn(TestUsers.Carl, connection ?? Server);
        Connection = user.RequireJira().Connection;
        RequestLog = new JiraRequestLog(System.IO.Path.Combine(Files.Path, "jira-logs"), Clock);
        Recorder = new JiraRequestRecorder(RequestLog, Feed, Clock, NullLogger<JiraRequestRecorder>.Instance);
        Http = HttpOver(Jira);
        Issues = new JiraIssueLookup(new JiraClient(Http), Clock);
        Calendar = new WorkCalendarService(Store, user);
        Legacy = new LegacyDataMigration(Store, Clock);
        var client = new JiraWorklogClient(Http, Clock);
        Queue = new RetryQueue(Store, user);
        Recent = new RecentWorklogIssues(Clock);
        Log = new TimeLog(client, Queue, user, Recent, Clock);
        Sync = new WorklogSync(client, Queue, Log, user, Clock);
    }

    public DateOnly Today => DateOnly.FromDateTime(Clock.Now.UtcDateTime);

    /// <summary>Another user's stored data, over the same store.</summary>
    public UserData For(UserKey key)
    {
        var user = TestUsers.SignedIn(key, Connection);
        return new UserData(new RetryQueue(Store, user), new WorkCalendarService(Store, user), new EpicExportStore(Store, user));
    }

    /// <summary>Signing in through the fake Jira; <paramref name="tab"/> becomes the user Jira confirms.</summary>
    public JiraIdentity Identity(CurrentUser tab) => new(new JiraClient(Http), Store, tab, Legacy, Clock);

    /// <summary>
    /// A Jira client registered the way Program.cs does it, sending to <paramref name="network"/>. Clients from one
    /// call share their pipelines (one per Jira host), as all tabs of the app do.
    /// </summary>
    public JiraHttp HttpOver(HttpMessageHandler network)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton(Recorder);
        services.AddJiraHttp().ConfigurePrimaryHttpMessageHandler(() => network);
        var provider = services.BuildServiceProvider();
        _providers.Add(provider);
        return provider.GetRequiredService<JiraHttp>();
    }

    /// <summary>A period loader and status book over the same fake Jira and store.</summary>
    public (PeriodLoader Loader, IssueStatuses Statuses) PeriodLoading() =>
        (new PeriodLoader(Log, Calendar, Issues, User), new IssueStatuses(Issues, new JiraClient(Http), User));

    /// <summary>A month export loader and the epic values store over the same fake Jira and store.</summary>
    public (MonthExportLoader Loader, EpicExportStore Values) ExportLoading()
    {
        var values = new EpicExportStore(Store, User);
        return (new MonthExportLoader(Log, Issues, values, User), values);
    }

    /// <summary>The day's work as the timesheet reads it.</summary>
    public async Task<IReadOnlyList<TimeEntry>> DayAsync(DateOnly day) => (await Log.GetRangeAsync(day, day)).Entries;

    /// <summary>
    /// Saves one day the way Save all does: <paramref name="rows"/> edited from what was read (<paramref name="saved"/>,
    /// read now when not given) are planned and sent to Jira.
    /// </summary>
    public async Task<WorklogSync.DayResult> SaveDayAsync(DateOnly day, IReadOnlyList<TimeEntry> rows, IReadOnlyList<TimeEntry>? saved = null)
    {
        var plan = DaySave.Plan(day, rows, saved ?? await DayAsync(day));
        return (await Sync.SaveDaysAsync([plan])).Days[day];
    }

    /// <summary>Editable copies of the day's work.</summary>
    public async Task<List<TimeEntry>> CopiesAsync(DateOnly day) => (await DayAsync(day)).Select(e => e.Copy()).ToList();

    /// <summary>Files in the retry queue, for any day.</summary>
    public Task<IReadOnlyList<TimeEntry>> QueuedAsync() => Queue.GetPendingAsync(DateOnly.MinValue, DateOnly.MaxValue);

    public int JiraSeconds(string key) =>
        Jira.Worklogs.TryGetValue(key, out var logs) ? logs.Values.Sum(l => (int)l["timeSpentSeconds"]!) : 0;

    public static TimeEntry Row(string key, double hours, string? comment = null) =>
        new() { IssueKey = key, IssueSummary = $"Summary of {key}", Duration = TimeSpan.FromHours(hours), Comment = comment };

    public void Dispose()
    {
        foreach (var provider in _providers) provider.Dispose();
        Files.Dispose();
    }
}

/// <summary>One user's stored data: the retry queue, days and work hours, and export values per epic.</summary>
public sealed record UserData(RetryQueue Queue, WorkCalendarService Calendar, EpicExportStore ExportValues);

/// <summary>Two users on <see cref="TimeHarness.Server"/>.</summary>
public static class TestUsers
{
    public static readonly UserKey Carl = UserKey.For(TimeHarness.Server.BaseUrl, "JIRAUSER1");
    public static readonly UserKey Anna = UserKey.For(TimeHarness.Server.BaseUrl, "JIRAUSER2");

    /// <summary>A tab signed in as <paramref name="key"/> (Carl when not given) without asking Jira.</summary>
    public static CurrentUser SignedIn(UserKey? key = null, JiraConnection? connection = null)
    {
        var user = new CurrentUser();
        key ??= Carl;
        user.Set(key, new JiraProfile(key.JiraUserId, null, key.JiraUserId, null),
            connection ?? new JiraConnection($"https://{key.Host}", JiraAuthType.Pat, "token", null), offline: false);
        return user;
    }
}

/// <summary>Reading one day's kind and hours, which the app always reads as part of a range.</summary>
public static class WorkCalendarReading
{
    public static async Task<DayInfo> GetDayAsync(this WorkCalendarService calendar, DateOnly day) =>
        (await calendar.GetDaysAsync(day, day))[day];
}

/// <summary>
/// A clock tests can set and move. Without a time it is noon UTC on Tuesday 27 October 2026, in UTC. Timers fire at
/// once and move the clock on to when they were due, so waits (such as a retry after a 429) take no real time
/// and tests can see how long they waited.
/// </summary>
public sealed class TestClock(DateTimeOffset now) : TimeProvider
{
    private readonly Lock _lock = new();
    private DateTimeOffset _now = now;

    public TestClock() : this(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero)) { }

    public DateTimeOffset Now
    {
        get { lock (_lock) return _now; }
        set { lock (_lock) _now = value; }
    }

    public override DateTimeOffset GetUtcNow() => Now;
    /// <summary>The server's time zone; UTC unless a test sets another.</summary>
    public TimeZoneInfo Zone { get; set; } = TimeZoneInfo.Utc;

    public override TimeZoneInfo LocalTimeZone => Zone;
    public void Advance(TimeSpan by) { lock (_lock) _now += by; }

    /// <summary>Moves the clock to <paramref name="time"/> unless it is already later: waits that overlap don't add up.</summary>
    private void MoveTo(DateTimeOffset time) { lock (_lock) if (time > _now) _now = time; }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
        new JumpingTimer(this, callback, state, dueTime);

    private sealed class JumpingTimer : ITimer
    {
        private readonly TestClock _clock;
        private readonly TimerCallback _callback;
        private readonly object? _state;

        public JumpingTimer(TestClock clock, TimerCallback callback, object? state, TimeSpan dueTime)
        {
            (_clock, _callback, _state) = (clock, callback, state);
            Change(dueTime, Timeout.InfiniteTimeSpan);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            if (dueTime == Timeout.InfiniteTimeSpan) return true;
            _clock.MoveTo(_clock.Now + dueTime);
            ThreadPool.QueueUserWorkItem(_ => _callback(_state));
            return true;
        }

        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
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
