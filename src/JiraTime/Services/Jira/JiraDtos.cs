using System.Text.Json;

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
internal record JiraErrorBody(List<string>? ErrorMessages, Dictionary<string, string>? Errors);
