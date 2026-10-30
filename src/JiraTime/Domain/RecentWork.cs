namespace JiraTime.Domain;

/// <summary>Quick picks from recent work: the issues and comments the user logged lately. Pure logic.</summary>
public static class RecentWork
{
    /// <summary>How far back recent issues and comments are taken from.</summary>
    public const int Days = 60;

    /// <summary>Most recently logged issues, newest first, each once.</summary>
    public static IReadOnlyList<JiraIssue> Issues(IEnumerable<TimeEntry> entries, int count = 8) =>
        Newest(entries)
            .DistinctBy(e => e.IssueKey, StringComparer.OrdinalIgnoreCase)
            .Take(count)
            .Select(e => new JiraIssue(e.IssueKey, e.IssueSummary))
            .ToList();

    /// <summary>The latest distinct comments per issue, newest first, at most <paramref name="perIssue"/> each.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Comments(IEnumerable<TimeEntry> entries, int perIssue = 5) =>
        entries
            .Where(e => TimeEntry.CleanComment(e.Comment) is not null)
            .GroupBy(e => e.IssueKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key,
                g => (IReadOnlyList<string>)Newest(g)
                    .Select(e => TimeEntry.CleanComment(e.Comment)!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(perIssue)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<TimeEntry> Newest(IEnumerable<TimeEntry> entries) =>
        entries.Where(e => e.IssueKey != "").OrderByDescending(e => e.Date).ThenByDescending(e => e.UpdatedUtc);
}
