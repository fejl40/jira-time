namespace JiraTime.Domain;

/// <summary>A titled group of issues in the issue picker.</summary>
public sealed record IssueGroup(string Title, IReadOnlyList<JiraIssue> Items);

/// <summary>
/// What the issue picker offers: the user's sprint, issues from earlier in the period, recent issues
/// and Jira search results, each issue listed once, in that order. Pure logic.
/// </summary>
public sealed class IssueChoices
{
    public const string SprintTitle = "My sprint", EarlierTitle = "Earlier this period", RecentTitle = "Recent", SearchTitle = "Jira search";

    public IReadOnlyList<IssueWithStatus> Sprint { get; init; } = [];
    /// <summary>Issues assigned to the user that were to do earlier in the period, maybe Done now.</summary>
    public IReadOnlyList<IssueWithStatus> Earlier { get; init; } = [];
    public IReadOnlyList<JiraIssue> Recent { get; init; } = [];
    /// <summary>Known current statuses by issue key, for issues in neither list.</summary>
    public IReadOnlyDictionary<string, IssueWithStatus> Statuses { get; init; } = new Dictionary<string, IssueWithStatus>();

    /// <summary>
    /// The non-empty groups for what the user typed (<paramref name="query"/>, null for nothing). Jira results
    /// only show once the query is long enough to search, and only issues not listed already.
    /// </summary>
    public IReadOnlyList<IssueGroup> For(string? query, IReadOnlyList<JiraIssue> jiraResults)
    {
        // An issue shows in the first list it is in, even when the query hides it there.
        var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<JiraIssue> Take(IEnumerable<JiraIssue> issues) =>
            IssueFilter.Filter(issues.Where(i => i.Key != "" && listed.Add(i.Key)).ToList(), query);

        var sprint = Take(Sprint.Select(s => s.ToIssue()));
        var earlier = Take(Earlier.Select(s => s.ToIssue()));
        var recent = Take(Recent);
        // Jira may match on more than key and summary, so its results only skip issues already shown.
        var shown = sprint.Concat(earlier).Concat(recent).Select(i => i.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<JiraIssue> jira = IssueFilter.ShouldSearchJira(query) ? jiraResults.Where(i => !shown.Contains(i.Key)).ToList() : [];

        var groups = new IssueGroup[] { new(SprintTitle, sprint), new(EarlierTitle, earlier), new(RecentTitle, recent), new(SearchTitle, jira) };
        return groups.Where(g => g.Items.Count > 0).ToList();
    }

    /// <summary>The issue's current status, when known.</summary>
    public IssueWithStatus? StatusOf(string key) =>
        Sprint.FirstOrDefault(s => Same(s.Key, key)) ?? Earlier.FirstOrDefault(s => Same(s.Key, key)) ?? Statuses.GetValueOrDefault(key);

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Filters issues as the user types in the issue picker. Pure logic.</summary>
public static class IssueFilter
{
    /// <summary>
    /// Issues where every word of <paramref name="query"/> is in the key or summary (case-insensitive),
    /// best first: exact key, then key starting with the query, then the rest in their original order.
    /// An empty query keeps every issue.
    /// </summary>
    public static IReadOnlyList<JiraIssue> Filter(IEnumerable<JiraIssue> issues, string? query)
    {
        var q = query?.Trim() ?? "";
        if (q == "") return issues.ToList();
        var words = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return issues
            .Select((issue, index) => (issue, index))
            .Where(x => words.All(w => x.issue.Key.Contains(w, StringComparison.OrdinalIgnoreCase)
                                       || x.issue.Summary.Contains(w, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => x.issue.Key.Equals(q, StringComparison.OrdinalIgnoreCase) ? 0
                : x.issue.Key.StartsWith(q, StringComparison.OrdinalIgnoreCase) ? 1 : 2)
            .ThenBy(x => x.index)
            .Select(x => x.issue)
            .ToList();
    }

    /// <summary>A Jira search is worth it once the query has two characters.</summary>
    public static bool ShouldSearchJira(string? query) => (query?.Trim().Length ?? 0) >= 2;
}
