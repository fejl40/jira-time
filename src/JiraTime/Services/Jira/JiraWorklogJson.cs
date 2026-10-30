using System.Globalization;
using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>Jira's worklog JSON and the app's worklogs: reading worklogs, writing the worklog body.</summary>
internal static class JiraWorklogJson
{
    public static JiraIssue ToIssue(SearchIssue hit) => new(hit.Key, hit.Fields?.Summary ?? "");

    /// <summary>Every worklog of the issue as the app keeps it; worklogs without an id or a readable day are left out.</summary>
    public static IEnumerable<JiraWorklog> All(IEnumerable<RawJiraWorklog> worklogs, JiraIssue issue)
    {
        foreach (var w in worklogs)
            if (ToWorklog(w, issue) is { } worklog) yield return worklog;
    }

    /// <summary>The worklog as the app keeps it; null when it has no id or its day can't be read.</summary>
    public static JiraWorklog? ToWorklog(RawJiraWorklog w, JiraIssue issue)
    {
        if (w.Id is null || ParseStarted(w.Started) is not { } date) return null;
        var comment = AtlassianDocument.ToPlainText(w.Comment);
        return new JiraWorklog(w.Id, issue.Key, issue.Summary, date, WholeMinutes(w.TimeSpentSeconds), comment, ParseCreated(w.Created));
    }

    /// <summary>Worklog time in whole minutes (at least one), the unit entries are kept in.</summary>
    public static TimeSpan WholeMinutes(int seconds) => TimeSpan.FromMinutes(Math.Max(1, Math.Round(seconds / 60.0)));

    public static bool IsAuthor(RawUser? author, JiraProfile profile) =>
        author is not null && (author.AccountId == profile.Id || author.Key == profile.Id
                               || (profile.Username is not null && author.Name == profile.Username));

    /// <summary>The day of a worklog's "started" value, as Jira wrote it (2026-10-19T09:00:00.000+0200).</summary>
    public static DateOnly? ParseStarted(string? started) =>
        started is { Length: >= 10 } && DateOnly.TryParseExact(started[..10], "yyyy-MM-dd", out var d) ? d : null;

    /// <summary>A worklog's "created" value, e.g. 2026-10-19T09:00:00.000+0200; null when it can't be read.</summary>
    public static DateTimeOffset? ParseCreated(string? created)
    {
        if (created is not { Length: > 5 }) return null;
        // Jira writes the offset without a colon (+0200), which .NET only reads with one.
        var text = created[^5] is '+' or '-' ? created.Insert(created.Length - 2, ":") : created;
        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }

    /// <summary>Jira's worklog "started" format, e.g. 2026-10-19T09:00:00.000+0200 (offset without a colon).</summary>
    public static string FormatStarted(DateOnly date, TimeOnly time, TimeZoneInfo zone)
    {
        var local = date.ToDateTime(time);
        var offset = zone.GetUtcOffset(local);
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        return $"{local:yyyy-MM-dd'T'HH:mm:ss}.000{sign}{Math.Abs(offset.Hours):00}{Math.Abs(offset.Minutes):00}";
    }

    /// <summary>The body that creates or overwrites a worklog with the entry's day, time and comment.</summary>
    /// <param name="zone">The user's time zone: the entry's start time is on the clock there.</param>
    public static object Body(TimeEntry e, bool isCloud, TimeZoneInfo zone)
    {
        var started = FormatStarted(e.Date, TimeEntry.JiraStartTime, zone);
        var seconds = (int)e.Duration.TotalSeconds;
        var text = e.Comment ?? "";
        // Cloud (API v3) wants comments as Atlassian Document Format.
        object comment = isCloud ? AtlassianDocument.FromPlainText(text) : text;
        return new { started, timeSpentSeconds = seconds, comment };
    }
}

/// <summary>Whose worklogs are wanted, and on which days.</summary>
internal sealed record WorklogRange(JiraProfile Profile, DateOnly From, DateOnly To)
{
    /// <summary>The profile's worklogs on the range's days.</summary>
    public IEnumerable<JiraWorklog> Mine(IEnumerable<RawJiraWorklog> worklogs, JiraIssue issue)
    {
        foreach (var w in worklogs)
        {
            if (!JiraWorklogJson.IsAuthor(w.Author, Profile) || JiraWorklogJson.ToWorklog(w, issue) is not { } worklog) continue;
            if (worklog.Date >= From && worklog.Date <= To) yield return worklog;
        }
    }
}
