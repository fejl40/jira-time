using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Storage;
using Microsoft.Extensions.DependencyInjection;
using JiraTime.Services.Jira;
using JiraTime.Services.JiraLog;
using JiraTime.Services.SignIn;
using JiraTime.Services.Worklogs;

namespace JiraTime.Tests.Support;

/// <summary>
/// The app's services as Program.cs registers them (<see cref="AppServices.AddJiraTime"/>), signed in as Carl, over a
/// fake Jira, a test clock and a temporary store that holds the retry queue, days off and settings.
/// </summary>
public sealed class TimeHarness : IDisposable
{
    public static readonly JiraConnection Server = new("https://jira.example.com", JiraAuthType.Pat, "token", null);
    public static readonly JiraConnection Cloud = new("https://acme.atlassian.net", JiraAuthType.Cloud, "token", "carl@example.com");

    public TempStore Files { get; } = new();
    public FileDocumentStore Store => Files.Store;
    public FakeJira Jira { get; } = new();
    public TestClock Clock { get; } = new();
    public JiraConnection Connection { get; }
    public CurrentUser User => Get<CurrentUser>();
    public RetryQueue Queue => Get<RetryQueue>();
    public TimeLog Log => Get<TimeLog>();
    public WorklogSync Sync => Get<WorklogSync>();
    public JiraIssueLookup Issues => Get<JiraIssueLookup>();
    public WorkCalendarService Calendar => Get<WorkCalendarService>();
    public LegacyDataMigration Legacy => Get<LegacyDataMigration>();
    public JiraRequestLog RequestLog => Get<JiraRequestLog>();
    public JiraRequestFeed Feed => Get<JiraRequestFeed>();
    /// <summary>The Jira client as the app registers it (resilience pipeline and request log), over <see cref="Jira"/>.</summary>
    public JiraHttp Http => Get<JiraHttp>();

    private readonly ServiceProvider _app;
    private readonly IServiceScope _tab;
    private readonly List<IDisposable> _disposables = [];

    public TimeHarness(JiraConnection? connection = null)
    {
        var services = new ServiceCollection().AddLogging().AddJiraTime(Files.Path);
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<IDocumentStore>(Store);
        services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => Jira));
        _app = services.BuildServiceProvider();
        _tab = Tab(TestUsers.Carl, connection ?? Server);
        Connection = User.RequireJira().Connection;
    }

    public DateOnly Today => DateOnly.FromDateTime(Clock.Now.UtcDateTime);

    /// <summary>One of the app's services, as Carl's tab gets it.</summary>
    public T Get<T>() where T : notnull => _tab.ServiceProvider.GetRequiredService<T>();

    /// <summary>Another user's stored data, over the same store.</summary>
    public UserData For(UserKey key)
    {
        var tab = Tab(key, Connection).ServiceProvider;
        return new(tab.GetRequiredService<RetryQueue>(), tab.GetRequiredService<WorkCalendarService>(), tab.GetRequiredService<EpicExportStore>());
    }

    /// <summary>Signing in through the fake Jira; <paramref name="tab"/> becomes the user Jira confirms.</summary>
    public JiraIdentity Identity(CurrentUser tab) => ActivatorUtilities.CreateInstance<JiraIdentity>(_tab.ServiceProvider, tab);

    /// <summary>Asks the fake Jira who the token of <paramref name="connection"/> (Carl's when not given) belongs to.</summary>
    public Task<JiraProfile> MyselfAsync(JiraConnection? connection = null) => Get<JiraClient>().GetMyselfAsync(connection ?? Connection);

    /// <summary>A Jira client as the app registers it, sending to <paramref name="network"/>; clients from one call share their pipelines.</summary>
    public JiraHttp HttpOver(HttpMessageHandler network)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton(Get<JiraRequestRecorder>());
        services.AddJiraHttp().ConfigurePrimaryHttpMessageHandler(() => network);
        var provider = services.BuildServiceProvider();
        _disposables.Add(provider);
        return provider.GetRequiredService<JiraHttp>();
    }

    /// <summary>A period loader and status book over the same fake Jira and store.</summary>
    public (PeriodLoader Loader, IssueStatuses Statuses) PeriodLoading() => (Get<PeriodLoader>(), Get<IssueStatuses>());

    /// <summary>A month export loader and the epic values store over the same fake Jira and store.</summary>
    public (MonthExportLoader Loader, EpicExportStore Values) ExportLoading() => (Get<MonthExportLoader>(), Get<EpicExportStore>());

    /// <summary>The day's work as the timesheet reads it.</summary>
    public async Task<IReadOnlyList<TimeEntry>> DayAsync(DateOnly day) => (await Log.GetRangeAsync(day, day)).Entries;

    /// <summary>Saves one day as Save all does: <paramref name="rows"/> edited from <paramref name="saved"/> (read now when not given).</summary>
    public async Task<WorklogSync.DayResult> SaveDayAsync(DateOnly day, IReadOnlyList<TimeEntry> rows, IReadOnlyList<TimeEntry>? saved = null)
    {
        var plan = DaySave.Plan(day, rows, saved ?? await DayAsync(day));
        return (await Sync.SaveDaysAsync([plan])).Days[day];
    }

    /// <summary>Editable copies of the day's work.</summary>
    public async Task<List<TimeEntry>> CopiesAsync(DateOnly day) => (await DayAsync(day)).Select(e => e.Copy()).ToList();

    /// <summary>Files in the retry queue, for any day.</summary>
    public Task<IReadOnlyList<TimeEntry>> QueuedAsync() => Queue.GetPendingAsync(DateOnly.MinValue, DateOnly.MaxValue);

    public int JiraSeconds(string key) => Jira.Worklogs.TryGetValue(key, out var logs) ? logs.Values.Sum(l => (int)l["timeSpentSeconds"]!) : 0;

    /// <summary>A new tab (DI scope) signed in as <paramref name="key"/> without asking Jira.</summary>
    private IServiceScope Tab(UserKey key, JiraConnection connection)
    {
        var tab = _app.CreateScope();
        _disposables.Add(tab);
        TestUsers.SignIn(tab.ServiceProvider.GetRequiredService<CurrentUser>(), key, connection);
        return tab;
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables) disposable.Dispose();
        _app.Dispose();
        Files.Dispose();
    }
}

/// <summary>One user's stored data: the retry queue, days and work hours, and export values per epic.</summary>
public sealed record UserData(RetryQueue Queue, WorkCalendarService Calendar, EpicExportStore ExportValues);

/// <summary>Reading one day's kind and hours, which the app always reads as part of a range.</summary>
public static class WorkCalendarReading
{
    public static async Task<DayInfo> GetDayAsync(this WorkCalendarService calendar, DateOnly day) =>
        (await calendar.GetDaysAsync(day, day))[day];
}
