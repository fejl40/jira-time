namespace JiraTime.Domain;

/// <summary>
/// Proposes entries for the hours a workday is missing. Pure logic: nothing is saved here.
/// </summary>
public static class DayFiller
{
    /// <summary>
    /// Draft entries that fill the day's free time inside the workday window (outside lunch), earliest
    /// gaps first, until the day has <see cref="Workday.Target"/>. Non-workdays get nothing.
    /// A draft whose issue could not be guessed has an empty <see cref="TimeEntry.IssueKey"/>.
    /// </summary>
    public static IReadOnlyList<TimeEntry> Propose(DayInfo day, Workday workday, IReadOnlyList<TimeEntry> logged, IReadOnlyList<SprintIssue> sprint)
    {
        if (!day.IsWorkday) return [];
        var missing = day.Expected - logged.Aggregate(TimeSpan.Zero, (t, e) => t + e.Duration);
        var drafts = new List<TimeEntry>();

        foreach (var (start, end) in Gaps(workday, logged))
        {
            if (missing <= TimeSpan.Zero) break;
            var length = end - start < missing ? end - start : missing;
            var issue = GuessIssue(start, end, workday, [.. logged, .. drafts], sprint);
            drafts.Add(new TimeEntry
            {
                IssueKey = issue?.Key ?? "",
                IssueSummary = issue?.Summary ?? "",
                Date = day.Date,
                Start = start,
                End = start.Add(length),
            });
            missing -= length;
        }
        return drafts;
    }

    /// <summary>
    /// <see cref="Propose"/> for every day up to and including <paramref name="today"/>; days that need
    /// nothing are left out.
    /// </summary>
    public static IReadOnlyDictionary<DateOnly, IReadOnlyList<TimeEntry>> ProposePeriod(
        IEnumerable<DayInfo> days, DateOnly today, Workday workday, IReadOnlyList<TimeEntry> logged, IReadOnlyList<SprintIssue> sprint) =>
        days.Where(d => d.Date <= today)
            .Select(d => (d.Date, Drafts: Propose(d, workday, logged.Where(e => e.Date == d.Date).ToList(), sprint)))
            .Where(p => p.Drafts.Count > 0)
            .ToDictionary(p => p.Date, p => p.Drafts);

    /// <summary>Free stretches of the workday window, outside lunch and outside <paramref name="logged"/>, in order.</summary>
    public static IReadOnlyList<(TimeOnly Start, TimeOnly End)> Gaps(Workday workday, IEnumerable<TimeEntry> logged)
    {
        var busy = logged.OrderBy(e => e.Start).ToList();
        var gaps = new List<(TimeOnly, TimeOnly)>();
        foreach (var (from, to) in new[] { (workday.Start, workday.LunchStart), (workday.LunchEnd, workday.End) })
        {
            var cursor = from;
            foreach (var e in busy.Where(e => e.End > from && e.Start < to))
            {
                if (e.Start > cursor) gaps.Add((cursor, e.Start));
                if (e.End > cursor) cursor = e.End;
            }
            if (cursor < to) gaps.Add((cursor, to));
        }
        return gaps;
    }

    /// <summary>
    /// Best guess for a gap: the issue logged right before it, then right after it (lunch counts as
    /// no break), then an In Progress sprint issue, then any sprint issue.
    /// </summary>
    public static JiraIssue? GuessIssue(TimeOnly gapStart, TimeOnly gapEnd, Workday workday, IReadOnlyList<TimeEntry> around, IReadOnlyList<SprintIssue> sprint)
    {
        bool EndsAt(TimeEntry e, TimeOnly t) => e.End == t || (t == workday.LunchEnd && e.End == workday.LunchStart);
        bool StartsAt(TimeEntry e, TimeOnly t) => e.Start == t || (t == workday.LunchStart && e.Start == workday.LunchEnd);

        var neighbour = around.Where(e => e.IssueKey != "").FirstOrDefault(e => EndsAt(e, gapStart))
                        ?? around.Where(e => e.IssueKey != "").FirstOrDefault(e => StartsAt(e, gapEnd));
        if (neighbour is not null) return new JiraIssue(neighbour.IssueKey, neighbour.IssueSummary);

        return (sprint.FirstOrDefault(i => i.IsInProgress) ?? sprint.FirstOrDefault())?.ToIssue();
    }
}
