using System.Globalization;
using System.Net;
using JiraTime.Domain;
using static JiraTime.Services.JiraHttp;

namespace JiraTime.Services;

/// <summary>Jira REST calls that read and write worklogs. Runs on the server, so there are no CORS issues.</summary>
public class JiraWorklogClient(JiraHttp jira)
{
    private readonly JiraHttp _jira = jira;

    /// <summary>
    /// Worklogs <paramref name="profile"/> logged on days from <paramref name="from"/> to <paramref name="to"/>,
    /// on any issue, however they were logged. The issues in <paramref name="alsoRead"/> are read even when
    /// Jira's search doesn't return them, which it can't for a while after a worklog was written.
    /// </summary>
    public async Task<IReadOnlyList<JiraWorklog>> GetMyWorklogsAsync(JiraConnection c, JiraProfile profile, DateOnly from, DateOnly to,
        IReadOnlyCollection<JiraIssue>? alsoRead = null, CancellationToken ct = default)
    {
        var jql = $"worklogAuthor = {Jql.User(profile)} AND worklogDate >= {Jql.Date(from)} AND worklogDate <= {Jql.Date(to)}";
        var issues = (await SearchAllAsync(c, jql, ct)).Select(i => new JiraIssue(i.Key, i.Fields?.Summary ?? "")).ToList();
        var found = issues.Select(i => i.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var extra = (alsoRead ?? []).Where(i => found.Add(i.Key)).ToList();

        var result = new List<JiraWorklog>();
        foreach (var issue in issues)
            result.AddRange(await ReadWorklogsAsync(c, issue, profile, from, to, ct));
        foreach (var issue in extra)
        {
            try
            {
                result.AddRange(await ReadWorklogsAsync(c, issue, profile, from, to, ct));
            }
            catch (JiraException ex) when (ex.Status == HttpStatusCode.NotFound)
            {
                // The issue is gone, and its worklogs with it.
            }
        }
        return result;
    }

    /// <summary>The issue's worklogs by <paramref name="profile"/> on days from <paramref name="from"/> to <paramref name="to"/>.</summary>
    private async Task<List<JiraWorklog>> ReadWorklogsAsync(JiraConnection c, JiraIssue issue, JiraProfile profile, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var result = new List<JiraWorklog>();
        for (var startAt = 0; ; )
        {
            var page = await _jira.GetAsync<WorklogPage>(c, $"{WorklogUrl(c, issue.Key)}?startAt={startAt}&maxResults=1000", ct);
            foreach (var w in page.Worklogs)
            {
                if (w.Id is null || !IsAuthor(w.Author, profile) || ToWorklog(w, w.Id, issue) is not { } worklog) continue;
                if (worklog.Date < from || worklog.Date > to) continue;
                result.Add(worklog);
            }
            startAt += page.Worklogs.Count;
            if (page.Worklogs.Count == 0 || startAt >= page.Total) break;
        }
        return result;
    }

    /// <summary>One worklog, or null when Jira no longer has it.</summary>
    public async Task<JiraWorklog?> GetWorklogAsync(JiraConnection c, string issueKey, string worklogId, CancellationToken ct = default)
    {
        try
        {
            var w = await _jira.GetAsync<RawJiraWorklog>(c, WorklogUrl(c, issueKey, worklogId), ct);
            return ToWorklog(w, w.Id ?? worklogId, new JiraIssue(issueKey, ""));
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>The worklog as the app keeps it; null when its day can't be read.</summary>
    private static JiraWorklog? ToWorklog(RawJiraWorklog w, string id, JiraIssue issue) =>
        ParseStarted(w.Started) is { } date
            ? new JiraWorklog(id, issue.Key, issue.Summary, date, WholeMinutes(w.TimeSpentSeconds), AtlassianDocument.ToPlainText(w.Comment), ParseCreated(w.Created))
            : null;

    /// <summary>The URL of the issue's worklogs, or of one of them.</summary>
    private static string WorklogUrl(JiraConnection c, string issueKey, string? worklogId = null) =>
        $"{Api(c)}/issue/{Uri.EscapeDataString(issueKey)}/worklog" + (worklogId is null ? "" : "/" + Uri.EscapeDataString(worklogId));

    /// <summary>Worklog time in whole minutes (at least one), the unit entries are kept in.</summary>
    public static TimeSpan WholeMinutes(int seconds) => TimeSpan.FromMinutes(Math.Max(1, Math.Round(seconds / 60.0)));

    private static bool IsAuthor(RawUser? author, JiraProfile profile) =>
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

    /// <summary>Every issue matching <paramref name="jql"/>, page by page.</summary>
    private async Task<List<SearchIssue>> SearchAllAsync(JiraConnection c, string jql, CancellationToken ct)
    {
        var all = new List<SearchIssue>();
        string? token = null;
        for (var page = 0; page < 50; page++)
        {
            var paging = c.IsCloud
                ? (token is null ? "" : $"&nextPageToken={Uri.EscapeDataString(token)}")
                : $"&startAt={all.Count}";
            var result = await _jira.GetAsync<SearchResult>(c,
                $"{SearchPath(c)}?jql={Uri.EscapeDataString(jql)}&fields=summary&maxResults=100{paging}", ct);
            all.AddRange(result.Issues);
            token = result.NextPageToken;
            var more = c.IsCloud ? token is not null && result.IsLast != true : result.Total is { } total && all.Count < total;
            if (!more || result.Issues.Count == 0) break;
        }
        return all;
    }

    /// <summary>Creates a worklog for <paramref name="entry"/> on its issue.</summary>
    /// <returns>The new worklog id.</returns>
    public async Task<string> AddWorklogAsync(JiraConnection c, TimeEntry entry, CancellationToken ct = default)
    {
        var created = await _jira.SendAsync<RawWorklog>(c, HttpMethod.Post,
            $"{WorklogUrl(c, entry.IssueKey)}?adjustEstimate=auto", WorklogBody(c, entry), ct);
        return created?.Id ?? throw new JiraException("Jira did not return a worklog id.");
    }

    /// <summary>Overwrites an existing worklog on <paramref name="issueKey"/> with the entry's time and comment.</summary>
    public Task UpdateWorklogAsync(JiraConnection c, string issueKey, string worklogId, TimeEntry entry, CancellationToken ct = default) =>
        _jira.SendAsync<RawWorklog>(c, HttpMethod.Put,
            $"{WorklogUrl(c, issueKey, worklogId)}?adjustEstimate=auto", WorklogBody(c, entry), ct);

    /// <summary>Deletes a worklog on <paramref name="issueKey"/>.</summary>
    public Task DeleteWorklogAsync(JiraConnection c, string issueKey, string worklogId, CancellationToken ct = default) =>
        _jira.SendAsync<object>(c, HttpMethod.Delete,
            $"{WorklogUrl(c, issueKey, worklogId)}?adjustEstimate=auto", null, ct);

    /// <summary>Jira's worklog "started" format, e.g. 2026-10-19T09:00:00.000+0200 (offset without a colon).</summary>
    public static string FormatStarted(DateOnly date, TimeOnly time, TimeZoneInfo? zone = null)
    {
        var local = date.ToDateTime(time);
        var offset = (zone ?? TimeZoneInfo.Local).GetUtcOffset(local);
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        return $"{local:yyyy-MM-dd'T'HH:mm:ss}.000{sign}{Math.Abs(offset.Hours):00}{Math.Abs(offset.Minutes):00}";
    }

    private static object WorklogBody(JiraConnection c, TimeEntry e)
    {
        var started = FormatStarted(e.Date, TimeEntry.JiraStartTime);
        var seconds = (int)e.Duration.TotalSeconds;
        var text = e.Description ?? "";
        // Cloud (API v3) wants comments as Atlassian Document Format.
        object comment = c.IsCloud ? AtlassianDocument.FromPlainText(text) : text;
        return new { started, timeSpentSeconds = seconds, comment };
    }
}
