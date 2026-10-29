using System.Net;
using System.Text.Json;
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
        var parentEpics = parents.Count == 0 ? new Dictionary<string, IssueEpic>() : await ReadEpicsAsync(c, parents, epicLink, ct);
        return found.ToDictionary(f => f.Key,
            f => f.Value.Epic ?? (f.Value.SubtaskOf is { } parent ? parentEpics.GetValueOrDefault(parent)?.Epic : null),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>What one issue says about its epic.</summary>
    /// <param name="Epic">Its epic's key, when the issue names one.</param>
    /// <param name="SubtaskOf">The parent's key, when the issue is a sub-task (whose epic is the parent's).</param>
    public sealed record IssueEpic(string? Epic, string? SubtaskOf);

    /// <summary>Reads <see cref="IssueEpic"/> from an issue's fields as Jira returns them.</summary>
    public static IssueEpic EpicOf(JsonElement fields, string? epicLinkField)
    {
        var parent = fields.TryGetProperty("parent", out var p) && p.ValueKind == JsonValueKind.Object ? p : (JsonElement?)null;
        var parentKey = parent is { } pk && pk.TryGetProperty("key", out var k) ? k.GetString() : null;
        if (parent is { } pe && IsEpic(pe.TryGetProperty("fields", out var pf) ? pf : default)) return new(parentKey, null);
        if (epicLinkField is not null && fields.TryGetProperty(epicLinkField, out var link) && link.ValueKind == JsonValueKind.String)
            return new(link.GetString(), null);
        var subtask = fields.TryGetProperty("issuetype", out var t) && t.ValueKind == JsonValueKind.Object
                      && t.TryGetProperty("subtask", out var st) && st.ValueKind == JsonValueKind.True;
        return new(null, subtask ? parentKey : null);
    }

    private static bool IsEpic(JsonElement fields) =>
        fields.ValueKind == JsonValueKind.Object && fields.TryGetProperty("issuetype", out var t) && t.ValueKind == JsonValueKind.Object
        && ((t.TryGetProperty("hierarchyLevel", out var level) && level.ValueKind == JsonValueKind.Number && level.GetInt32() == 1)
            || (t.TryGetProperty("name", out var name) && string.Equals(name.GetString(), "Epic", StringComparison.OrdinalIgnoreCase)));

    private async Task<Dictionary<string, IssueEpic>> ReadEpicsAsync(JiraConnection c, IReadOnlyCollection<string> keys, string? epicLink, CancellationToken ct)
    {
        if (keys.Count == 0) return new(StringComparer.OrdinalIgnoreCase);
        var fields = "issuetype,parent" + (epicLink is null ? "" : "," + epicLink);
        try
        {
            var result = await _jira.GetAsync<JsonElement>(c,
                $"{SearchPath(c)}?jql={Uri.EscapeDataString($"key in {Jql.List(keys)}")}&fields={fields}&maxResults=100", ct);
            var found = new Dictionary<string, IssueEpic>(StringComparer.OrdinalIgnoreCase);
            if (result.TryGetProperty("issues", out var issues) && issues.ValueKind == JsonValueKind.Array)
                foreach (var issue in issues.EnumerateArray())
                    if (issue.TryGetProperty("key", out var key) && key.GetString() is { } k)
                        found[k] = issue.TryGetProperty("fields", out var f) ? EpicOf(f, epicLink) : new(null, null);
            return found;
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.BadRequest && keys.Count > 1)
        {
            // One unknown key fails the whole query: ask one by one.
            var found = new Dictionary<string, IssueEpic>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in keys)
                foreach (var (k, v) in await ReadEpicsAsync(c, [key], epicLink, ct)) found[k] = v;
            return found;
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.BadRequest)
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>The id of the Epic Link field (e.g. customfield_10008) on Jiras that have one (Server/DC); null otherwise.</summary>
    public async Task<string?> GetEpicLinkFieldAsync(JiraConnection c, CancellationToken ct = default)
    {
        var all = await _jira.GetAsync<JsonElement>(c, $"{Api(c)}/field", ct);
        if (all.ValueKind != JsonValueKind.Array) return null;
        return all.EnumerateArray()
            .Where(f => (f.TryGetProperty("schema", out var schema) && schema.ValueKind == JsonValueKind.Object
                         && schema.TryGetProperty("custom", out var custom) && custom.GetString() == "com.pyxis.greenhopper.jira:gh-epic-link")
                        || (f.TryGetProperty("name", out var name) && name.GetString() == "Epic Link"))
            .Select(f => f.TryGetProperty("id", out var id) ? id.GetString() : null)
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
