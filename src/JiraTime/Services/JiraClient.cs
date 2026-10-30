using System.Net;
using System.Text.Json;
using JiraTime.Domain;
using static JiraTime.Services.JiraHttp;

namespace JiraTime.Services;

/// <summary>Jira REST calls about issues and the user. Runs on the server, so there are no CORS issues.</summary>
public class JiraClient(JiraHttp jira)
{
    private readonly JiraHttp _jira = jira;

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
    public async Task<IReadOnlyList<IssueWithStatus>> GetIssuesAsync(JiraConnection c, IReadOnlyCollection<string> keys, CancellationToken ct = default) =>
        await ByKeysAsync(keys, some => SearchAsync(c, $"key in {Jql.List(some)}", ct));

    /// <summary>
    /// The epic of each issue, by key; null for issues with no epic (and for epics themselves). The epic is
    /// the parent when that is an epic, else the Epic Link field (Server/DC); a sub-task has its parent's epic.
    /// Keys Jira doesn't know are left out.
    /// </summary>
    /// <param name="epicLink">The Epic Link field's id from <see cref="GetEpicLinkFieldAsync"/>, if the Jira has one.</param>
    public async Task<IReadOnlyDictionary<string, string?>> GetEpicsAsync(JiraConnection c, IReadOnlyCollection<string> keys, string? epicLink, CancellationToken ct = default)
    {
        var found = await ReadEpicsAsync(c, keys, epicLink, ct);
        // A sub-task's epic is its parent's.
        var parents = found.Values.Where(f => f.Epic is null && f.SubtaskOf is not null).Select(f => f.SubtaskOf!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var parentEpics = await ReadEpicsAsync(c, parents, epicLink, ct);
        return found.ToDictionary(f => f.Key,
            f => f.Value.Epic ?? (f.Value.SubtaskOf is { } parent ? parentEpics.GetValueOrDefault(parent)?.Epic : null),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>What one issue says about its epic.</summary>
    /// <param name="Epic">Its epic's key, when the issue names one.</param>
    /// <param name="SubtaskOf">The parent's key, when the issue is a sub-task (whose epic is the parent's).</param>
    private sealed record IssueEpic(string? Epic, string? SubtaskOf);

    /// <summary>Reads <see cref="IssueEpic"/> from an issue's fields as Jira returns them.</summary>
    private static IssueEpic EpicOf(EpicFields? fields, string? epicLinkField)
    {
        var parent = fields?.Parent;
        if (parent is not null && IsEpic(parent.Fields?.IssueType)) return new(parent.Key, null);
        if (epicLinkField is not null && fields?.Other?.TryGetValue(epicLinkField, out var link) == true && link.ValueKind == JsonValueKind.String)
            return new(link.GetString(), null);
        return new(null, fields?.IssueType?.Subtask == true ? parent?.Key : null);
    }

    private static bool IsEpic(IssueTypeField? type) =>
        type is not null && (type.HierarchyLevel == 1 || string.Equals(type.Name, "Epic", StringComparison.OrdinalIgnoreCase));

    private async Task<Dictionary<string, IssueEpic>> ReadEpicsAsync(JiraConnection c, IReadOnlyCollection<string> keys, string? epicLink, CancellationToken ct)
    {
        var found = await ByKeysAsync(keys, some => SearchEpicsAsync(c, some, epicLink, ct));
        return found.DistinctBy(f => f.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(f => f.Key, f => f.Epic, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<IReadOnlyList<(string Key, IssueEpic Epic)>> SearchEpicsAsync(JiraConnection c, IReadOnlyCollection<string> keys, string? epicLink, CancellationToken ct)
    {
        var fields = "issuetype,parent" + (epicLink is null ? "" : "," + epicLink);
        var result = await _jira.GetAsync<EpicSearchResult>(c,
            $"{SearchPath(c)}?jql={Uri.EscapeDataString($"key in {Jql.List(keys)}")}&fields={fields}&maxResults=100", ct);
        return result.Issues
            .Where(i => i.Key is not null)
            .Select(i => (i.Key!, EpicOf(i.Fields, epicLink)))
            .ToList();
    }

    /// <summary>
    /// Searches for issues by key. One key Jira doesn't know fails the whole query, so then each key is
    /// asked on its own; a single unknown key gives nothing.
    /// </summary>
    private static async Task<IReadOnlyList<T>> ByKeysAsync<T>(IReadOnlyCollection<string> keys, Func<IReadOnlyCollection<string>, Task<IReadOnlyList<T>>> search)
    {
        if (keys.Count == 0) return [];
        try
        {
            return await search(keys);
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.BadRequest && keys.Count > 1)
        {
            var found = new List<T>();
            foreach (var key in keys)
                found.AddRange(await ByKeysAsync([key], search));
            return found;
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.BadRequest)
        {
            return [];
        }
    }

    /// <summary>The id of the Epic Link field (e.g. customfield_10008) on Jiras that have one (Server/DC); null otherwise.</summary>
    public async Task<string?> GetEpicLinkFieldAsync(JiraConnection c, CancellationToken ct = default)
    {
        var all = await _jira.GetAsync<List<RawField>>(c, $"{Api(c)}/field", ct);
        return all
            .Where(f => f.Schema?.Custom == "com.pyxis.greenhopper.jira:gh-epic-link" || f.Name == "Epic Link")
            .Select(f => f.Id)
            .FirstOrDefault(id => id is not null);
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
