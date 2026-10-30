using System.Text.Json;
using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>Jira's issue, user and field JSON and the app's issues, profile and epics.</summary>
internal static class JiraIssueJson
{
    public static JiraProfile ToProfile(RawUser u) =>
        new(u.AccountId ?? u.Key ?? u.Name ?? "", u.Name, u.DisplayName ?? u.Name ?? "?", u.EmailAddress);

    public static IssueWithStatus ToIssueWithStatus(SearchIssue i) =>
        new(i.Key, i.Fields?.Summary ?? "", i.Fields?.IssueType?.Name ?? "", i.Fields?.Status?.Name ?? "", i.Fields?.Status?.StatusCategory?.Key ?? "");

    /// <summary>The issue picker's answer as one list: each issue once, whichever sections name it.</summary>
    public static IReadOnlyList<JiraIssue> ToIssues(PickerResult result) =>
        result.Sections
            .SelectMany(s => s.Issues)
            .DistinctBy(i => i.Key)
            .Select(i => new JiraIssue(i.Key, i.SummaryText ?? ""))
            .ToList();

    /// <summary>The id of the Epic Link field (e.g. customfield_10008) among Jira's fields; null when there is none.</summary>
    public static string? EpicLinkField(IEnumerable<RawField> fields) =>
        fields
            .Where(f => f.Schema?.Custom == "com.pyxis.greenhopper.jira:gh-epic-link" || f.Name == "Epic Link")
            .Select(f => f.Id)
            .FirstOrDefault(id => id is not null);

    /// <summary>What each issue says about its epic, by key.</summary>
    public static Dictionary<string, IssueEpic> EpicsOf(IEnumerable<EpicSearchIssue> found, string? epicLinkField) =>
        found
            .Where(i => i.Key is not null)
            .DistinctBy(i => i.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(i => i.Key!, i => EpicOf(i.Fields, epicLinkField), StringComparer.OrdinalIgnoreCase);

    /// <summary>The sub-tasks' parents whose epic is needed: those of sub-tasks that name no epic themselves.</summary>
    public static List<string> ParentsToRead(IReadOnlyDictionary<string, IssueEpic> found) =>
        found.Values
            .Where(f => f.Epic is null && f.SubtaskOf is not null)
            .Select(f => f.SubtaskOf!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>The epic of each issue, by key: its own, else (for a sub-task) its parent's.</summary>
    public static IReadOnlyDictionary<string, string?> Epics(IReadOnlyDictionary<string, IssueEpic> found, IReadOnlyDictionary<string, IssueEpic> parents) =>
        found.ToDictionary(f => f.Key,
            f => f.Value.Epic ?? (f.Value.SubtaskOf is { } parent ? parents.GetValueOrDefault(parent)?.Epic : null),
            StringComparer.OrdinalIgnoreCase);

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
}

/// <summary>What one issue says about its epic.</summary>
/// <param name="Epic">Its epic's key, when the issue names one.</param>
/// <param name="SubtaskOf">The parent's key, when the issue is a sub-task (whose epic is the parent's).</param>
internal sealed record IssueEpic(string? Epic, string? SubtaskOf);
