using System.Net;
using JiraTime.Domain;
using static JiraTime.Services.JiraHttp;

namespace JiraTime.Services;

/// <summary>Jira REST calls. Runs on the server, so there are no CORS issues.</summary>
public class JiraClient(HttpClient http)
{
    private readonly JiraHttp _jira = new(http);

    /// <summary>The user the token belongs to. Doubles as a token check.</summary>
    public async Task<JiraProfile> GetMyselfAsync(JiraConnection c, CancellationToken ct = default) =>
        ToProfile(await _jira.GetAsync<RawUser>(c, $"{Api(c)}/myself", ct));

    /// <summary>Issue search for autocomplete. Uses the issue picker, which exists on both Cloud and Server/DC.</summary>
    public async Task<IReadOnlyList<JiraIssue>> SearchIssuesAsync(JiraConnection c, string query, CancellationToken ct = default)
    {
        var result = await _jira.GetAsync<PickerResult>(c,
            $"/rest/api/2/issue/picker?query={Uri.EscapeDataString(query)}&showSubTasks=true", ct);
        return result.Sections
            .SelectMany(s => s.Issues)
            .DistinctBy(i => i.Key)
            .Select(i => new JiraIssue(i.Key, i.SummaryText ?? ""))
            .ToList();
    }

    /// <summary>
    /// Issues assigned to <paramref name="profile"/> in open sprints that are not Done (tasks, stories, ...).
    /// Falls back to all assigned, not-Done issues when the instance has no sprints (no Jira Software).
    /// </summary>
    public async Task<IReadOnlyList<SprintIssue>> GetSprintIssuesAsync(JiraConnection c, JiraProfile profile, CancellationToken ct = default)
    {
        var mine = $"assignee = {Jql.User(profile)} AND statusCategory != Done";
        try
        {
            return await SearchAsync(c, $"{mine} AND sprint in openSprints() ORDER BY updated DESC", ct);
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.BadRequest)
        {
            return await SearchAsync(c, $"{mine} ORDER BY updated DESC", ct);
        }
    }

    /// <summary>
    /// Issues assigned to <paramref name="profile"/> at some time from <paramref name="from"/> to
    /// <paramref name="to"/> that were in To Do or In Progress then, whatever their status is now.
    /// Jira's history search needs status names; if Jira rejects them, falls back to issues assigned
    /// during the period that weren't resolved before it.
    /// </summary>
    public async Task<IReadOnlyList<SprintIssue>> GetPeriodIssuesAsync(JiraConnection c, JiraProfile profile, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var during = $"DURING ({Jql.Date(from)}, {Jql.Date(to)})";
        var assigned = $"assignee WAS {Jql.User(profile)} {during}";
        try
        {
            return await SearchAsync(c, $"{assigned} AND status WAS IN {Jql.List(["To Do", "In Progress"])} {during} ORDER BY updated DESC", ct);
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.BadRequest)
        {
            return await SearchAsync(c, $"{assigned} AND (resolution IS EMPTY OR resolutiondate >= {Jql.Date(from)}) ORDER BY updated DESC", ct);
        }
    }

    /// <summary>Current summary and status of the given issues. Keys Jira doesn't know are left out.</summary>
    public async Task<IReadOnlyList<SprintIssue>> GetIssuesAsync(JiraConnection c, IReadOnlyCollection<string> keys, CancellationToken ct = default)
    {
        if (keys.Count == 0) return [];
        try
        {
            return await SearchAsync(c, $"key in {Jql.List(keys)}", ct);
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.BadRequest && keys.Count > 1)
        {
            // One unknown key fails the whole query: ask one by one.
            var found = new List<SprintIssue>();
            foreach (var key in keys)
                found.AddRange(await GetIssuesAsync(c, [key], ct));
            return found;
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.BadRequest)
        {
            return [];
        }
    }

    private async Task<IReadOnlyList<SprintIssue>> SearchAsync(JiraConnection c, string jql, CancellationToken ct)
    {
        var result = await _jira.GetAsync<SearchResult>(c,
            $"{SearchPath(c)}?jql={Uri.EscapeDataString(jql)}&fields=summary,issuetype,status&maxResults=100", ct);
        return result.Issues
            .Select(i => new SprintIssue(i.Key, i.Fields?.Summary ?? "", i.Fields?.IssueType?.Name ?? "",
                i.Fields?.Status?.Name ?? "", i.Fields?.Status?.StatusCategory?.Key ?? ""))
            .ToList();
    }

    /// <summary>
    /// Worklogs <paramref name="profile"/> logged on days from <paramref name="from"/> to <paramref name="to"/>,
    /// on any issue, however they were logged.
    /// </summary>
    public async Task<IReadOnlyList<JiraWorklog>> GetMyWorklogsAsync(JiraConnection c, JiraProfile profile, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var jql = $"worklogAuthor = {Jql.User(profile)} AND worklogDate >= {Jql.Date(from)} AND worklogDate <= {Jql.Date(to)}";
        var result = new List<JiraWorklog>();
        foreach (var issue in await SearchAllAsync(c, jql, ct))
        {
            for (var startAt = 0; ; )
            {
                var page = await _jira.GetAsync<WorklogPage>(c,
                    $"{Api(c)}/issue/{Uri.EscapeDataString(issue.Key)}/worklog?startAt={startAt}&maxResults=1000", ct);
                foreach (var w in page.Worklogs)
                {
                    if (w.Id is null || !IsAuthor(w.Author, profile) || ParseStarted(w.Started) is not { } date) continue;
                    if (date < from || date > to) continue;
                    result.Add(new JiraWorklog(w.Id, issue.Key, issue.Fields?.Summary ?? "", date,
                        WholeMinutes(w.TimeSpentSeconds), AtlassianDocument.ToPlainText(w.Comment)));
                }
                startAt += page.Worklogs.Count;
                if (page.Worklogs.Count == 0 || startAt >= page.Total) break;
            }
        }
        return result;
    }

    /// <summary>One worklog, or null when Jira no longer has it.</summary>
    public async Task<JiraWorklog?> GetWorklogAsync(JiraConnection c, string issueKey, string worklogId, CancellationToken ct = default)
    {
        try
        {
            var w = await _jira.GetAsync<RawJiraWorklog>(c,
                $"{Api(c)}/issue/{Uri.EscapeDataString(issueKey)}/worklog/{Uri.EscapeDataString(worklogId)}", ct);
            return ParseStarted(w.Started) is { } date
                ? new JiraWorklog(w.Id ?? worklogId, issueKey, "", date, WholeMinutes(w.TimeSpentSeconds), AtlassianDocument.ToPlainText(w.Comment))
                : null;
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>Worklog time in whole minutes (at least one), the unit entries are kept in.</summary>
    public static TimeSpan WholeMinutes(int seconds) => TimeSpan.FromMinutes(Math.Max(1, Math.Round(seconds / 60.0)));

    private static bool IsAuthor(RawUser? author, JiraProfile profile) =>
        author is not null && (author.AccountId == profile.Id || author.Key == profile.Id
                               || (profile.Username is not null && author.Name == profile.Username));

    /// <summary>The day of a worklog's "started" value, as Jira wrote it (2026-10-19T09:00:00.000+0200).</summary>
    public static DateOnly? ParseStarted(string? started) =>
        started is { Length: >= 10 } && DateOnly.TryParseExact(started[..10], "yyyy-MM-dd", out var d) ? d : null;

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
            $"{Api(c)}/issue/{Uri.EscapeDataString(entry.IssueKey)}/worklog?adjustEstimate=auto", WorklogBody(c, entry), ct);
        return created?.Id ?? throw new JiraException("Jira did not return a worklog id.");
    }

    /// <summary>Overwrites an existing worklog on <paramref name="issueKey"/> with the entry's time and comment.</summary>
    public Task UpdateWorklogAsync(JiraConnection c, string issueKey, string worklogId, TimeEntry entry, CancellationToken ct = default) =>
        _jira.SendAsync<RawWorklog>(c, HttpMethod.Put,
            $"{Api(c)}/issue/{Uri.EscapeDataString(issueKey)}/worklog/{Uri.EscapeDataString(worklogId)}?adjustEstimate=auto", WorklogBody(c, entry), ct);

    public Task DeleteWorklogAsync(JiraConnection c, string issueKey, string worklogId, CancellationToken ct = default) =>
        _jira.SendAsync<object>(c, HttpMethod.Delete,
            $"{Api(c)}/issue/{Uri.EscapeDataString(issueKey)}/worklog/{Uri.EscapeDataString(worklogId)}?adjustEstimate=auto", null, ct);

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

    private static JiraProfile ToProfile(RawUser u) =>
        new(u.AccountId ?? u.Key ?? u.Name ?? "", u.Name, u.DisplayName ?? u.Name ?? "?", u.EmailAddress);
}
