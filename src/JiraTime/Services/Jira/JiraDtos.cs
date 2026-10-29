using System.Text.Json;
using System.Text.Json.Serialization;

namespace JiraTime.Services;

// Raw JSON shapes of Jira's REST answers, only the fields the app reads.

internal record RawUser(string? AccountId, string? Key, string? Name, string? DisplayName, string? EmailAddress);
internal record PickerResult(List<PickerSection> Sections);
internal record PickerSection(List<PickerIssue> Issues);
internal record PickerIssue(string Key, string? SummaryText);
internal record RawWorklog(string? Id);
internal record SearchResult(List<SearchIssue> Issues, int? Total = null, string? NextPageToken = null, bool? IsLast = null);
internal record WorklogPage(List<RawJiraWorklog> Worklogs, int Total);
internal record RawJiraWorklog(string? Id, string? Started, int TimeSpentSeconds, JsonElement? Comment, RawUser? Author, string? Created = null);
internal record SearchIssue(string Key, SearchFields? Fields);
internal record SearchFields(string? Summary, NamedField? IssueType, StatusField? Status);
internal record NamedField(string? Name);
internal record StatusField(string? Name, StatusCategoryField? StatusCategory);
internal record StatusCategoryField(string? Key);
internal record EpicSearchResult(List<EpicSearchIssue> Issues);
internal record EpicSearchIssue(string? Key, EpicFields? Fields);
internal record EpicFields(EpicParent? Parent, IssueTypeField? IssueType)
{
    /// <summary>The other fields asked for, among them the Epic Link field, whose id differs per Jira.</summary>
    [JsonExtensionData] public Dictionary<string, JsonElement>? Other { get; init; }
}
internal record EpicParent(string? Key, EpicParentFields? Fields);
internal record EpicParentFields(IssueTypeField? IssueType);
internal record IssueTypeField(string? Name, int? HierarchyLevel, bool Subtask);
internal record RawField(string? Id, string? Name, RawFieldSchema? Schema);
internal record RawFieldSchema(string? Custom);
internal record JiraErrorBody(List<string>? ErrorMessages, Dictionary<string, string>? Errors);
