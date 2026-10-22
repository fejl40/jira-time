namespace JiraTime.Domain;

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
