namespace JiraTime.Domain;

/// <summary>
/// Suggests entries for the hours a workday is missing. Pure logic: nothing is saved here.
/// </summary>
public static class DayFiller
{
    /// <summary>
    /// One draft entry with the day's missing hours (up to <see cref="DayInfo.Target"/>) on the
    /// best-guess issue. Non-workdays and full days get nothing. When no issue can be guessed the
    /// draft's <see cref="TimeEntry.IssueKey"/> is empty.
    /// </summary>
    public static IReadOnlyList<TimeEntry> Propose(DayInfo day, IReadOnlyList<TimeEntry> logged, IReadOnlyList<SprintIssue> sprint)
    {
        var missing = day.Expected - logged.Aggregate(TimeSpan.Zero, (t, e) => t + e.Duration);
        if (missing <= TimeSpan.Zero) return [];

        var issue = GuessIssue(logged, sprint);
        return
        [
            new TimeEntry
            {
                IssueKey = issue?.Key ?? "",
                IssueSummary = issue?.Summary ?? "",
                Date = day.Date,
                Duration = missing,
            },
        ];
    }

    /// <summary>
    /// <see cref="Propose"/> for every day up to and including <paramref name="today"/>; days that need
    /// nothing are left out.
    /// </summary>
    public static IReadOnlyDictionary<DateOnly, IReadOnlyList<TimeEntry>> ProposePeriod(
        IEnumerable<DayInfo> days, DateOnly today, IReadOnlyList<TimeEntry> logged, IReadOnlyList<SprintIssue> sprint) =>
        days.Where(d => d.Date <= today)
            .Select(d => (d.Date, Drafts: Propose(d, logged.Where(e => e.Date == d.Date).ToList(), sprint)))
            .Where(p => p.Drafts.Count > 0)
            .ToDictionary(p => p.Date, p => p.Drafts);

    /// <summary>
    /// Best guess for the rest of a day: the issue last worked on that day, then an In Progress
    /// sprint issue, then any sprint issue.
    /// </summary>
    public static JiraIssue? GuessIssue(IReadOnlyList<TimeEntry> sameDay, IReadOnlyList<SprintIssue> sprint)
    {
        if (sameDay.Where(e => e.IssueKey != "").MaxBy(e => e.UpdatedUtc) is { } last)
            return new JiraIssue(last.IssueKey, last.IssueSummary);
        return (sprint.FirstOrDefault(i => i.IsInProgress) ?? sprint.FirstOrDefault())?.ToIssue();
    }
}
