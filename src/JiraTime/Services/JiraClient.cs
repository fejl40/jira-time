using System.Net;
using JiraTime.Domain;
using static JiraTime.Services.JiraHttp;

namespace JiraTime.Services;

/// <summary>Jira REST calls about issues and the user. Runs on the server, so there are no CORS issues.</summary>
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
    public async Task<IReadOnlyList<IssueWithStatus>> GetSprintIssuesAsync(JiraConnection c, JiraProfile profile, CancellationToken ct = default)
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
    public async Task<IReadOnlyList<IssueWithStatus>> GetPeriodIssuesAsync(JiraConnection c, JiraProfile profile, DateOnly from, DateOnly to, CancellationToken ct = default)
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
    public async Task<IReadOnlyList<IssueWithStatus>> GetIssuesAsync(JiraConnection c, IReadOnlyCollection<string> keys, CancellationToken ct = default)
    {
        if (keys.Count == 0) return [];
        try
        {
            return await SearchAsync(c, $"key in {Jql.List(keys)}", ct);
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.BadRequest && keys.Count > 1)
        {
            // One unknown key fails the whole query: ask one by one.
            var found = new List<IssueWithStatus>();
            foreach (var key in keys)
                found.AddRange(await GetIssuesAsync(c, [key], ct));
            return found;
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.BadRequest)
        {
            return [];
        }
    }

    private async Task<IReadOnlyList<IssueWithStatus>> SearchAsync(JiraConnection c, string jql, CancellationToken ct)
    {
        var result = await _jira.GetAsync<SearchResult>(c,
            $"{SearchPath(c)}?jql={Uri.EscapeDataString(jql)}&fields=summary,issuetype,status&maxResults=100", ct);
        return result.Issues
            .Select(i => new IssueWithStatus(i.Key, i.Fields?.Summary ?? "", i.Fields?.IssueType?.Name ?? "",
                i.Fields?.Status?.Name ?? "", i.Fields?.Status?.StatusCategory?.Key ?? ""))
            .ToList();
    }

    private static JiraProfile ToProfile(RawUser u) =>
        new(u.AccountId ?? u.Key ?? u.Name ?? "", u.Name, u.DisplayName ?? u.Name ?? "?", u.EmailAddress);
}
