using JiraTime.Domain;
using JiraTime.Services.SignIn;

namespace JiraTime.Tests.Support;

/// <summary>Time entries as the tests build them. Without a date they are on <see cref="Monday"/>, 19 October 2026.</summary>
public static class TestEntries
{
    public static readonly DateOnly Monday = new(2026, 10, 19);

    /// <summary>Work Jira has, as read from worklog <paramref name="worklog"/> on the entry's issue.</summary>
    public static TimeEntry Saved(string key, double hours, DateOnly? date = null, string? comment = null, string worklog = "1") => new()
    {
        IssueKey = key, IssueSummary = Summary(key), Date = date ?? Monday, Duration = TimeSpan.FromHours(hours), Comment = comment,
        JiraWorklogId = worklog, JiraWorklogIssueKey = key, SyncPending = false,
    };

    /// <summary>Work on a day that has never been sent to Jira.</summary>
    public static TimeEntry Draft(string key, double hours, DateOnly? date = null, string? comment = null) => new()
    {
        IssueKey = key, IssueSummary = Summary(key), Date = date ?? Monday, Duration = TimeSpan.FromHours(hours), Comment = comment,
    };

    /// <summary>Work waiting in the retry queue because Jira didn't take it (yet).</summary>
    public static TimeEntry Waiting(string key, double hours, DateOnly? date = null) => new()
    {
        IssueKey = key, IssueSummary = Summary(key), Date = date ?? Monday, Duration = TimeSpan.FromHours(hours), SyncPending = true,
    };

    /// <summary>A row as the user typed it: issue, hours and comment, not on a day yet (saving puts it on one).</summary>
    public static TimeEntry Row(string key, double hours, string? comment = null) =>
        new() { IssueKey = key, IssueSummary = Summary(key), Duration = TimeSpan.FromHours(hours), Comment = comment };

    /// <summary>The summary FakeJira gives an issue it was told nothing about.</summary>
    public static string Summary(string key) => $"Summary of {key}";
}

/// <summary>Period sheets loaded as the timesheet loads them, without Jira.</summary>
public static class TestSheets
{
    /// <summary>The sheet of <paramref name="today"/>'s period with <paramref name="saved"/> loaded; every day is a plain one.</summary>
    public static PeriodSheet For(DateOnly today, params TimeEntry[] saved)
    {
        var sheet = new PeriodSheet(Period.For(today), () => today);
        sheet.Load(saved, [], Days(sheet.Period));
        return sheet;
    }

    /// <summary>The kind and hours of every day of <paramref name="period"/>, with <paramref name="off"/> marked off.</summary>
    public static Dictionary<DateOnly, DayInfo> Days(Period period, params DayOff[] off)
    {
        var daysOff = off.ToDictionary(o => o.Date);
        return period.Days.ToDictionary(d => d, d => DayInfo.For(d, daysOff));
    }
}

/// <summary>Two users on <see cref="TimeHarness.Server"/>, and tabs signed in without asking Jira.</summary>
public static class TestUsers
{
    public static readonly UserKey Carl = UserKey.For(TimeHarness.Server.BaseUrl, "JIRAUSER1");
    public static readonly UserKey Anna = UserKey.For(TimeHarness.Server.BaseUrl, "JIRAUSER2");

    /// <summary>Carl as Jira Server describes him.</summary>
    public static readonly JiraProfile CarlsProfile = new("JIRAUSER1", "carl", "Carl Lilholm", null);

    /// <summary>A tab signed in as <paramref name="key"/> (Carl when not given) without asking Jira.</summary>
    public static CurrentUser SignedIn(UserKey? key = null, JiraConnection? connection = null) =>
        SignIn(new CurrentUser(), key ?? Carl, connection ?? new JiraConnection($"https://{(key ?? Carl).Host}", JiraAuthType.Pat, "token", null));

    /// <summary>Signs <paramref name="user"/> in as <paramref name="key"/> without asking Jira.</summary>
    public static CurrentUser SignIn(CurrentUser user, UserKey key, JiraConnection connection)
    {
        user.Set(key, new JiraProfile(key.JiraUserId, null, key.JiraUserId, null), connection, offline: false);
        return user;
    }
}
