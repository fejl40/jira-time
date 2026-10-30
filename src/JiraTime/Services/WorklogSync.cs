using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>
/// Writes work to Jira. Jira is where logged time lives: saving a day creates, updates and deletes its
/// worklogs directly. When Jira can't be reached the change is kept in the local retry queue
/// (<see cref="RetryQueue"/>), shown as not in Jira with the reason, and sent by <see cref="SyncRangeAsync"/>.
/// </summary>
public class WorklogSync(JiraWorklogClient jira, RetryQueue queue, TimeLog log, TimeProvider clock)
{
    private readonly WorklogWriter _writer = new(jira, queue, log);

    /// <summary>Outcome of <see cref="SaveDayAsync"/>.</summary>
    /// <param name="Errors">Row id → why it is invalid; <see cref="DayError"/> for the day as a whole. Empty when saved.</param>
    /// <param name="SyncFailures">Changes Jira did not get (yet); they wait in the retry queue.</param>
    /// <param name="ChangedInJira">Nothing was saved because Jira's worklogs changed since the day was read.</param>
    public record DayResult(IReadOnlyDictionary<string, string> Errors, int SyncFailures, bool ChangedInJira = false)
    {
        public const string DayError = DaySave.DayError;
        public bool Saved => Errors.Count == 0;
    }

    public const string ChangedInJiraMessage =
        "This day's worklogs were changed in Jira since you opened it, so nothing was saved. The day now shows Jira's version with your changes on top; check them and save again.";

    /// <summary>
    /// Makes the day's work match <paramref name="rows"/>: new rows are created in Jira, changed rows
    /// updated, and work in <paramref name="saved"/> missing from the rows deleted. Every row is validated
    /// first; if any is invalid, or a worklog to change was changed in Jira since it was read, nothing is written.
    /// </summary>
    /// <param name="saved">The day's work as it was read, which the rows were edited from.</param>
    public Task<DayResult> SaveDayAsync(JiraConnection c, DateOnly date, IReadOnlyList<TimeEntry> rows, IReadOnlyList<TimeEntry> saved,
        CancellationToken ct = default) =>
        SaveAsync(c, DaySave.Plan(date, rows, saved), inJira: null, ct);

    /// <summary>Saves a planned day.</summary>
    /// <param name="inJira">The checked worklogs as Jira has them now, when they were read already (Save all reads every day's at once).</param>
    private async Task<DayResult> SaveAsync(JiraConnection c, DaySave plan, WorklogsInJira? inJira, CancellationToken ct)
    {
        if (plan.Errors.Count > 0) return new DayResult(plan.Errors, 0);

        inJira ??= await ReadWorklogsAsync(c, plan.ToCheck, ct);
        if (inJira.ChangedSinceRead(plan.ToCheck))
        {
            log.Refresh();
            return new DayResult(new Dictionary<string, string> { [DayResult.DayError] = ChangedInJiraMessage }, 0, ChangedInJira: true);
        }

        var failures = 0;
        foreach (var e in plan.Removed)
            if (await _writer.DeleteAsync(c, e, ct) is not null) failures++;
        foreach (var row in plan.RowsToSend(clock.GetUtcNow().UtcDateTime))
            if (await _writer.PushAsync(c, row, ct) is not null) failures++;
        return new DayResult(plan.Errors, failures);
    }

    /// <summary>Outcome of <see cref="SaveDaysAsync"/>.</summary>
    /// <param name="Saved">Days saved, with how each went.</param>
    /// <param name="NeedsWork">Days not saved because a row needs an issue or hours.</param>
    /// <param name="ChangedInJira">Days not saved because their worklogs changed in Jira meanwhile.</param>
    public record SaveAllResult(IReadOnlyDictionary<DateOnly, DayResult> Saved, IReadOnlyList<DateOnly> NeedsWork, IReadOnlyList<DateOnly> ChangedInJira)
    {
        /// <summary>The day to show the user, the one <see cref="Notice"/> is about; null when every day was saved.</summary>
        public DateOnly? FirstToFix => ChangedInJira.Count > 0 ? ChangedInJira[0] : NeedsWork.Count > 0 ? NeedsWork[0] : null;

        /// <summary>What to tell the user about the days not saved; null when every day was saved.</summary>
        public string? Notice =>
            ChangedInJira.Count > 0
                ? $"{Days(ChangedInJira.Count)} not saved because the worklogs were changed in Jira meanwhile. Each shows Jira's version with your changes on top; check it and press Save all."
                : NeedsWork.Count > 0
                    ? $"{Days(NeedsWork.Count)} not saved because a row needs an issue or hours. Finish it below and press Save all."
                    : null;

        private static string Days(int n) => n == 1 ? "1 day" : $"{n} days";
    }

    /// <summary>
    /// Saves every day of the sheet with unsaved changes, each as <see cref="SaveDayAsync"/> does, but with one
    /// changed-in-Jira check for all days. Days with hours that can't be read are left for the user to finish.
    /// The sheet itself is not changed.
    /// </summary>
    public async Task<SaveAllResult> SaveDaysAsync(JiraConnection c, PeriodSheet sheet, CancellationToken ct = default)
    {
        var plans = new Dictionary<DateOnly, DaySave>();
        var needsWork = new List<DateOnly>();
        foreach (var day in sheet.UnsavedDays)
        {
            var work = sheet.WorkFor(day);
            var before = sheet.SavedOn(day);
            if (work.BadHours.Count == 0) plans[day] = DaySave.Plan(day, work.RowsToSave(before), before);
            else needsWork.Add(day);
        }

        // One search checks the worklogs of every day before any day is written.
        var toCheck = plans.Values.Where(p => p.Errors.Count == 0).SelectMany(p => p.ToCheck).ToList();
        var inJira = await ReadWorklogsAsync(c, toCheck, ct);

        var saved = new Dictionary<DateOnly, DayResult>();
        var changedInJira = new List<DateOnly>();
        foreach (var (day, plan) in plans)
        {
            var result = await SaveAsync(c, plan, inJira, ct);
            if (result.Saved) saved[day] = result;
            else if (result.ChangedInJira) changedInJira.Add(day);
            else needsWork.Add(day);
        }
        needsWork.Sort();
        return new SaveAllResult(saved, needsWork, changedInJira);
    }

    /// <summary>
    /// The worklogs a save would change, as Jira has them now, read with their issues in one search. When Jira can't
    /// be asked the save goes ahead: its changes then wait in the retry queue.
    /// </summary>
    private async Task<WorklogsInJira> ReadWorklogsAsync(JiraConnection c, IReadOnlyList<TimeEntry> read, CancellationToken ct)
    {
        if (read.Count == 0) return WorklogsInJira.Unknown;
        try
        {
            var issues = read.Select(e => e.WorklogIssueKey).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return new WorklogsInJira(await jira.GetWorklogsOnAsync(c, issues, ct));
        }
        catch (JiraException)
        {
            return WorklogsInJira.Unknown;
        }
    }

    /// <summary>Worklogs as Jira has them now, by id; <see cref="Unknown"/> when Jira couldn't be asked.</summary>
    private sealed record WorklogsInJira(IReadOnlyDictionary<string, JiraWorklog>? ById)
    {
        public static readonly WorklogsInJira Unknown = new((IReadOnlyDictionary<string, JiraWorklog>?)null);

        /// <summary>
        /// True when one of the worklogs is gone from Jira or no longer has the day, hours and comment it was read with.
        /// Never when Jira couldn't be asked.
        /// </summary>
        public bool ChangedSinceRead(IReadOnlyList<TimeEntry> read) =>
            ById is not null && read.Any(e => !ById.TryGetValue(e.JiraWorklogId!, out var now) || !e.Matches(now));
    }

    /// <summary>Sends every change in the range that waits in the retry queue.</summary>
    /// <returns>How many are still waiting afterwards.</returns>
    public async Task<int> SyncRangeAsync(JiraConnection c, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var failed = 0;
        foreach (var e in await queue.GetPendingAsync(from, to))
            if (await _writer.PushAsync(c, e, ct) is not null) failed++;
        return failed;
    }
}
