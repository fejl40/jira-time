using System.Net;
using JiraTime.Domain;
using static JiraTime.Services.JiraHttp;

namespace JiraTime.Services;

/// <summary>Jira REST calls that read and write worklogs. Runs on the server, so there are no CORS issues.</summary>
/// <param name="clock">Its time zone is where worklogs start when the connection has no browser time zone: Jira gets the start time with that zone's offset.</param>
public class JiraWorklogClient(JiraHttp jira, TimeProvider clock)
{
    /// <summary>
    /// Worklogs <paramref name="profile"/> logged on days from <paramref name="from"/> to <paramref name="to"/>,
    /// on any issue, however they were logged. The issues in <paramref name="alsoRead"/> are read even when
    /// Jira's worklog search doesn't return them, which it can't for a while after a worklog was written.
    /// </summary>
    public async Task<IReadOnlyList<JiraWorklog>> GetMyWorklogsAsync(JiraConnection c, JiraProfile profile, DateOnly from, DateOnly to,
        IReadOnlyCollection<JiraIssue>? alsoRead = null, CancellationToken ct = default)
    {
        var jql = $"worklogAuthor = {Jql.User(profile)} AND worklogDate >= {Jql.Date(from)} AND worklogDate <= {Jql.Date(to)}";
        var found = await jira.SearchAllAsync<SearchIssue>(c, jql, WorklogFields, ct);
        var keys = found.Select(i => i.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        // A search by key reads the issues themselves, so their worklogs are there before the worklog search finds them.
        var notFound = (alsoRead ?? []).Where(i => keys.Add(i.Key)).Select(i => i.Key).ToList();
        if (notFound.Count > 0) found.AddRange(await jira.ByKeysAsync<SearchIssue>(c, notFound, WorklogFields, ct));

        var range = new WorklogRange(profile, from, to);
        // Only an issue with more worklogs than the search sent is read on its own; those go out together.
        var perIssue = found.Select(async hit => range.Mine(await WorklogsOfAsync(c, hit, ct), JiraWorklogJson.ToIssue(hit)).ToList());
        return (await Task.WhenAll(perIssue)).SelectMany(w => w).ToList();
    }

    /// <summary>
    /// Every worklog on the given issues, by worklog id, read with the issues in one search (more when an issue has more
    /// worklogs than the search sends). Worklogs and issues Jira no longer has are left out.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, JiraWorklog>> GetWorklogsOnAsync(JiraConnection c, IReadOnlyCollection<string> issueKeys,
        CancellationToken ct = default)
    {
        var found = await jira.ByKeysAsync<SearchIssue>(c, issueKeys, WorklogFields, ct);
        var perIssue = found.Select(async hit => JiraWorklogJson.All(await WorklogsOfAsync(c, hit, ct), JiraWorklogJson.ToIssue(hit)).ToList());
        var result = new Dictionary<string, JiraWorklog>();
        foreach (var worklog in (await Task.WhenAll(perIssue)).SelectMany(w => w)) result[worklog.Id] = worklog;
        return result;
    }

    private const string WorklogFields = "summary,worklog";

    /// <summary>The issue's worklogs from the search answer when it sent them all, else read from Jira; none when the issue is gone.</summary>
    private async Task<IReadOnlyList<RawJiraWorklog>> WorklogsOfAsync(JiraConnection c, SearchIssue hit, CancellationToken ct)
    {
        if (hit.Fields?.Worklog is { IsComplete: true } embedded) return embedded.Worklogs;
        try
        {
            return await ReadWorklogsAsync(c, hit.Key, ct);
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.NotFound)
        {
            return [];
        }
    }

    /// <summary>Every worklog of the issue, read page by page.</summary>
    private async Task<List<RawJiraWorklog>> ReadWorklogsAsync(JiraConnection c, string issueKey, CancellationToken ct)
    {
        var result = new List<RawJiraWorklog>();
        for (var startAt = 0; ; )
        {
            var page = await jira.GetAsync<WorklogPage>(c, $"{WorklogUrl(c, issueKey)}?startAt={startAt}&maxResults=1000", ct);
            result.AddRange(page.Worklogs);
            startAt += page.Worklogs.Count;
            if (page.Worklogs.Count == 0 || startAt >= page.Total) break;
        }
        return result;
    }

    /// <summary>The URL of the issue's worklogs, or of one of them.</summary>
    private static string WorklogUrl(JiraConnection c, string issueKey, string? worklogId = null) =>
        $"{Api(c)}/issue/{Uri.EscapeDataString(issueKey)}/worklog" + (worklogId is null ? "" : "/" + Uri.EscapeDataString(worklogId));

    /// <summary>Creates a worklog for <paramref name="entry"/> on its issue.</summary>
    /// <returns>The new worklog id.</returns>
    public async Task<string> AddWorklogAsync(JiraConnection c, TimeEntry entry, CancellationToken ct = default)
    {
        var created = await jira.SendAsync<RawWorklog>(c, HttpMethod.Post,
            $"{WorklogUrl(c, entry.IssueKey)}?adjustEstimate=auto", Body(c, entry), ct);
        return created?.Id ?? throw new JiraException("Jira did not return a worklog id.");
    }

    /// <summary>Overwrites an existing worklog on <paramref name="issueKey"/> with the entry's time and comment.</summary>
    public Task UpdateWorklogAsync(JiraConnection c, string issueKey, string worklogId, TimeEntry entry, CancellationToken ct = default) =>
        jira.SendAsync<RawWorklog>(c, HttpMethod.Put,
            $"{WorklogUrl(c, issueKey, worklogId)}?adjustEstimate=auto", Body(c, entry), ct);

    /// <summary>Deletes a worklog on <paramref name="issueKey"/>.</summary>
    public Task DeleteWorklogAsync(JiraConnection c, string issueKey, string worklogId, CancellationToken ct = default) =>
        jira.SendAsync<object>(c, HttpMethod.Delete,
            $"{WorklogUrl(c, issueKey, worklogId)}?adjustEstimate=auto", null, ct);

    private object Body(JiraConnection c, TimeEntry entry) => JiraWorklogJson.Body(entry, c.IsCloud, c.TimeZone ?? clock.LocalTimeZone);
}
