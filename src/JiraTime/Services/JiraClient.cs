using System.Net;
using JiraTime.Domain;
using static JiraTime.Services.JiraHttp;

namespace JiraTime.Services;

/// <summary>Jira REST calls about issues and the user. Runs on the server, so there are no CORS issues.</summary>
public class JiraClient(JiraHttp jira)
{
    /// <summary>The user the token belongs to. Doubles as a token check.</summary>
    public async Task<JiraProfile> GetMyselfAsync(JiraConnection c, CancellationToken ct = default) =>
        JiraIssueJson.ToProfile(await jira.GetAsync<RawUser>(c, $"{Api(c)}/myself", ct));

    /// <summary>Issue search for autocomplete. Uses the issue picker, which exists on both Cloud and Server/DC.</summary>
    public async Task<IReadOnlyList<JiraIssue>> SearchIssuesAsync(JiraConnection c, string query, CancellationToken ct = default)
    {
        var result = await jira.GetAsync<PickerResult>(c,
            $"/rest/api/2/issue/picker?query={Uri.EscapeDataString(query)}&showSubTasks=true", ct);
        return JiraIssueJson.ToIssues(result);
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
    public async Task<IReadOnlyList<IssueWithStatus>> GetIssuesAsync(JiraConnection c, IReadOnlyCollection<string> keys, CancellationToken ct = default) =>
        (await jira.ByKeysAsync<SearchIssue>(c, keys, StatusFields, ct)).Select(JiraIssueJson.ToIssueWithStatus).ToList();

    /// <summary>
    /// The epic of each issue, by key; null for issues with no epic (and for epics themselves). The epic is
    /// the parent when that is an epic, else the Epic Link field (Server/DC); a sub-task has its parent's epic.
    /// Keys Jira doesn't know are left out.
    /// </summary>
    /// <param name="epicLink">The Epic Link field's id from <see cref="GetEpicLinkFieldAsync"/>, if the Jira has one.</param>
    public async Task<IReadOnlyDictionary<string, string?>> GetEpicsAsync(JiraConnection c, IReadOnlyCollection<string> keys, string? epicLink, CancellationToken ct = default)
    {
        var found = await ReadEpicsAsync(c, keys, epicLink, ct);
        var parents = await ReadEpicsAsync(c, JiraIssueJson.ParentsToRead(found), epicLink, ct);
        return JiraIssueJson.Epics(found, parents);
    }

    private async Task<Dictionary<string, IssueEpic>> ReadEpicsAsync(JiraConnection c, IReadOnlyCollection<string> keys, string? epicLink, CancellationToken ct)
    {
        var fields = "issuetype,parent" + (epicLink is null ? "" : "," + epicLink);
        return JiraIssueJson.EpicsOf(await jira.ByKeysAsync<EpicSearchIssue>(c, keys, fields, ct), epicLink);
    }

    /// <summary>The id of the Epic Link field (e.g. customfield_10008) on Jiras that have one (Server/DC); null otherwise.</summary>
    public async Task<string?> GetEpicLinkFieldAsync(JiraConnection c, CancellationToken ct = default) =>
        JiraIssueJson.EpicLinkField(await jira.GetAsync<List<RawField>>(c, $"{Api(c)}/field", ct));

    private const string StatusFields = "summary,issuetype,status";

    private async Task<IReadOnlyList<IssueWithStatus>> SearchAsync(JiraConnection c, string jql, CancellationToken ct) =>
        (await jira.SearchAllAsync<SearchIssue>(c, jql, StatusFields, ct)).Select(JiraIssueJson.ToIssueWithStatus).ToList();
}
