namespace JiraTime.Domain;

/// <summary>
/// Suggests entries for the hours a workday is missing. Pure logic: nothing is saved here.
/// </summary>
public static class DayFiller
{
    /// <summary>How far back to look for a previous worked day to copy.</summary>
    public const int LookBackDays = 14;

    /// <summary>Copies are rounded to this step.</summary>
    public static readonly TimeSpan Step = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Drafts for the day's missing hours (<see cref="DayInfo.Expected"/> minus what is logged).
    /// An empty day copies <paramref name="previous"/> (the rows of the previous worked day), scaled
    /// to the missing hours; otherwise, and when there is nothing to copy, one draft goes on the
    /// best-guess issue. Non-workdays and full days get nothing. When no issue can be guessed the
    /// draft's <see cref="TimeEntry.IssueKey"/> is empty.
    /// </summary>
    public static IReadOnlyList<TimeEntry> Propose(DayInfo day, IReadOnlyList<TimeEntry> logged, IReadOnlyList<SprintIssue> sprint,
        IReadOnlyList<TimeEntry>? previous = null)
    {
        var total = logged.Aggregate(TimeSpan.Zero, (t, e) => t + e.Duration);
        var missing = day.Expected - total;
        if (missing <= TimeSpan.Zero) return [];

        if (total == TimeSpan.Zero && previous is not null && Copy(day.Date, previous, missing) is { Count: > 0 } copy)
            return copy;

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
    /// The issues of <paramref name="source"/> as new rows on <paramref name="date"/>, split like the
    /// source and scaled to <paramref name="target"/> hours in quarter hours (null keeps the source hours).
    /// Comments are not copied.
    /// </summary>
    public static IReadOnlyList<TimeEntry> Copy(DateOnly date, IReadOnlyList<TimeEntry> source, TimeSpan? target = null)
    {
        var issues = source
            .Where(e => e.IssueKey != "" && e.Duration > TimeSpan.Zero)
            .GroupBy(e => e.IssueKey)
            .Select(g => (Key: g.Key, Summary: g.First().IssueSummary, Hours: g.Aggregate(TimeSpan.Zero, (t, e) => t + e.Duration)))
            .ToList();
        var total = issues.Aggregate(TimeSpan.Zero, (t, i) => t + i.Hours);
        if (total == TimeSpan.Zero || target <= TimeSpan.Zero) return [];

        var want = target ?? total;
        var shares = issues.Select(i => RoundToStep(i.Hours * (want / total))).ToList();
        // Put the rounding difference on the biggest share so the copy adds up exactly.
        var biggest = shares.IndexOf(shares.Max());
        shares[biggest] += want - shares.Aggregate(TimeSpan.Zero, (t, s) => t + s);

        return issues.Zip(shares)
            .Where(x => x.Second > TimeSpan.Zero)
            .Select(x => new TimeEntry { IssueKey = x.First.Key, IssueSummary = x.First.Summary, Date = date, Duration = x.Second })
            .ToList();
    }

    private static TimeSpan RoundToStep(TimeSpan t) =>
        TimeSpan.FromTicks((long)Math.Round(t.Ticks / (double)Step.Ticks, MidpointRounding.AwayFromZero) * Step.Ticks);

    /// <summary>
    /// The rows of the latest day before <paramref name="date"/> (up to <see cref="LookBackDays"/> back)
    /// that has hours, from <paramref name="rowsOn"/>; null when there is none.
    /// </summary>
    public static (DateOnly Date, IReadOnlyList<TimeEntry> Rows)? PreviousWorkedDay(DateOnly date, Func<DateOnly, IReadOnlyList<TimeEntry>> rowsOn)
    {
        for (var back = 1; back <= LookBackDays; back++)
        {
            var day = date.AddDays(-back);
            var rows = rowsOn(day);
            if (rows.Any(e => e.IssueKey != "" && e.Duration > TimeSpan.Zero)) return (day, rows);
        }
        return null;
    }

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
