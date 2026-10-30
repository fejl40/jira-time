using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>
/// Writes work to Jira. Jira is where logged time lives: saving a day creates, updates and deletes its
/// worklogs directly. When Jira can't be reached the change is kept in the local retry queue
/// (<see cref="RetryQueue"/>), shown as not in Jira with the reason, and sent by <see cref="SyncRangeAsync"/>.
/// </summary>
public class WorklogSync(JiraWorklogClient jira, RetryQueue queue, TimeLog log, CurrentUser user, TimeProvider clock)
{
    private readonly WorklogWriter _writer = new(jira, queue, log);

    /// <summary>How saving one day went.</summary>
    /// <param name="Errors">Row id → why it is invalid; <see cref="DaySave.DayError"/> for the day as a whole. Empty when saved.</param>
    /// <param name="SyncFailures">Changes Jira did not get (yet); they wait in the retry queue.</param>
    /// <param name="ChangedInJira">Nothing was saved because Jira's worklogs changed since the day was read.</param>
    public record DayResult(IReadOnlyDictionary<string, string> Errors, int SyncFailures, bool ChangedInJira = false)
    {
        public bool Saved => Errors.Count == 0;
    }

    public const string ChangedInJiraMessage =
        "This day's worklogs were changed in Jira since you opened it, so nothing was saved. The day now shows Jira's version with your changes on top; check them and save again.";

    /// <summary>Outcome of <see cref="SaveDaysAsync"/>: how each planned day went, in the order planned.</summary>
    public record SaveAllResult(IReadOnlyDictionary<DateOnly, DayResult> Days)
    {
        /// <summary>Days saved, with how each went.</summary>
        public IReadOnlyDictionary<DateOnly, DayResult> Saved => Days.Where(d => d.Value.Saved).ToDictionary();

        /// <summary>Days not saved because a row needs an issue or hours.</summary>
        public IReadOnlyList<DateOnly> NeedsWork => Days.Where(d => !d.Value.Saved && !d.Value.ChangedInJira).Select(d => d.Key).Order().ToList();

        /// <summary>Days not saved because their worklogs changed in Jira meanwhile.</summary>
        public IReadOnlyList<DateOnly> ChangedInJira => Days.Where(d => d.Value.ChangedInJira).Select(d => d.Key).ToList();

        /// <summary>The day to show the user, the one <see cref="Notice"/> is about; null when every day was saved.</summary>
        public DateOnly? FirstToFix => ChangedInJira.Count > 0 ? ChangedInJira[0] : NeedsWork.Count > 0 ? NeedsWork[0] : null;

        /// <summary>What to tell the user about the days not saved; null when every day was saved.</summary>
        public string? Notice =>
            ChangedInJira.Count > 0
                ? $"{Count(ChangedInJira.Count)} not saved because the worklogs were changed in Jira meanwhile. Each shows Jira's version with your changes on top; check it and press Save all."
                : NeedsWork.Count > 0
                    ? $"{Count(NeedsWork.Count)} not saved because a row needs an issue or hours. Finish it below and press Save all."
                    : null;

        private static string Count(int n) => n == 1 ? "1 day" : $"{n} days";
    }

    /// <summary>
    /// Saves planned days (<see cref="PeriodSheet.PlanSaveAll"/>): for each, new rows are created in Jira, changed rows
    /// updated and removed work deleted. A day whose plan has errors is left for the user to finish. One search first
    /// checks the worklogs of every day; a day with a worklog changed in Jira since it was read writes nothing.
    /// </summary>
    /// <exception cref="JiraException">Nobody is signed in to Jira.</exception>
    public async Task<SaveAllResult> SaveDaysAsync(IReadOnlyList<DaySave> plans, CancellationToken ct = default)
    {
        var c = user.RequireJira().Connection;
        // Plans with errors have nothing to check.
        var inJira = await ReadWorklogsAsync(c, plans.SelectMany(p => p.ToCheck).ToList(), ct);
        var results = new Dictionary<DateOnly, DayResult>();
        foreach (var plan in plans) results[plan.Date] = await SaveAsync(c, plan, inJira, ct);
        return new SaveAllResult(results);
    }

    /// <summary>Saves one planned day.</summary>
    /// <param name="inJira">The checked worklogs as Jira has them now.</param>
    private async Task<DayResult> SaveAsync(JiraConnection c, DaySave plan, WorklogsInJira inJira, CancellationToken ct)
    {
        if (plan.Errors.Count > 0) return new DayResult(plan.Errors, 0);
        if (inJira.ChangedSinceRead(plan.ToCheck))
        {
            log.Refresh();
            return new DayResult(new Dictionary<string, string> { [DaySave.DayError] = ChangedInJiraMessage }, 0, ChangedInJira: true);
        }

        var failures = 0;
        foreach (var e in plan.Removed)
            if (await _writer.DeleteAsync(c, e, ct) is not null) failures++;
        foreach (var row in plan.RowsToSend(clock.GetUtcNow().UtcDateTime))
            if (await _writer.PushAsync(c, row, ct) is not null) failures++;
        return new DayResult(plan.Errors, failures);
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
    /// <exception cref="JiraException">Nobody is signed in to Jira.</exception>
    public async Task<int> SyncRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var c = user.RequireJira().Connection;
        var failed = 0;
        foreach (var e in await queue.GetPendingAsync(from, to))
            if (await _writer.PushAsync(c, e, ct) is not null) failed++;
        return failed;
    }
}
