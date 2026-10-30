namespace JiraTime.Domain;

/// <summary>Where a row of the day editor stands.</summary>
public enum RowState
{
    /// <summary>A saved row marked for removal; saving deletes it.</summary>
    Removed,
    /// <summary>A row the user added.</summary>
    New,
    /// <summary>A row suggested for missing hours, untouched so far.</summary>
    Suggested,
    /// <summary>A saved row the user edited.</summary>
    Changed,
    InJira,
    /// <summary>A saved row Jira doesn't have yet (it waits in the retry queue).</summary>
    NotInJira,
}

/// <summary>A row of a day's work as the day editor shows it: the entry as edited, the stored entry (null for a new row), and its state.</summary>
public sealed record WorkRow(TimeEntry Entry, TimeEntry? Saved, RowState State)
{
    public bool IsDraft => Saved is null;
    public bool Removed => State == RowState.Removed;

    /// <summary>Jira's reason for not taking the row is worth showing (not once the user changed or removed it).</summary>
    public bool ShowsSyncError => State == RowState.NotInJira && Entry.SyncError is not null;
}

/// <summary>
/// One day of a <see cref="PeriodSheet"/>: its kind and hours, its saved rows and its unsaved changes, read and
/// edited together. Made by <see cref="PeriodSheet.Day"/>: it holds the saved rows loaded last, so get a new one after
/// the period is read again; the unsaved changes are always the sheet's current ones.
/// </summary>
public sealed class SheetDay
{
    private readonly PeriodSheet _sheet;
    /// <summary>Stands in for the day's work until the first edit, so reading a day leaves nothing behind.</summary>
    private readonly DayWork _noWork;

    internal SheetDay(PeriodSheet sheet, DateOnly date)
    {
        (_sheet, Date, _noWork) = (sheet, date, new DayWork(date));
        Info = sheet.Info(date);
        Saved = sheet.SavedOn(date);
    }

    /// <summary>The day's unsaved changes as they stand in the sheet.</summary>
    private DayWork Work => _sheet.WorkOn(Date) ?? _noWork;

    /// <summary>The day's unsaved changes to edit, made in the sheet on first use.</summary>
    private DayWork Changing => _sheet.WorkToChange(Date);

    public DateOnly Date { get; }
    public DayInfo? Info { get; }
    /// <summary>The day's saved entries, as read.</summary>
    public IReadOnlyList<TimeEntry> Saved { get; }

    /// <summary>Hours the day should have (zero on weekends, holidays and days off).</summary>
    public TimeSpan Expected => Info?.Expected ?? TimeSpan.Zero;
    /// <summary>Hours saved on the day.</summary>
    public TimeSpan Logged => TimeEntry.Total(Saved);
    /// <summary>Saved hours still missing; only counted up to today.</summary>
    public TimeSpan Missing =>
        !_sheet.HoursKnown || Date > _sheet.Today || Info is null ? TimeSpan.Zero : Info.MissingWith(Logged);
    /// <summary>Saved hours as a share of the hours the day needs, from 0 to 1; 0 on days that need none.</summary>
    public double Progress => Expected > TimeSpan.Zero ? Math.Min(1, Logged / Expected) : 0;
    /// <summary>A day that needs hours and has them all saved.</summary>
    public bool IsComplete => Expected > TimeSpan.Zero && Progress >= 1;
    /// <summary>The latest earlier day with work, offered to copy.</summary>
    public (DateOnly Date, IReadOnlyList<TimeEntry> Rows)? PreviousWorkedDay => _sheet.PreviousWorkedDay(Date);

    /// <summary>The rows as the editor shows them: saved rows (as edited, or removed), then new rows.</summary>
    public IReadOnlyList<WorkRow> Rows
    {
        get
        {
            var rows = Saved.Select(s => new WorkRow(Work.View(s), s, SavedState(s))).ToList();
            rows.AddRange(Work.Drafts.Select(d => new WorkRow(d, null, Work.IsSuggested(d) ? RowState.Suggested : RowState.New)));
            return rows;
        }
    }

    private RowState SavedState(TimeEntry saved) =>
        Work.IsRemoved(saved) ? RowState.Removed
        : Work.IsChanged(saved) ? RowState.Changed
        : Work.View(saved).IsSynced ? RowState.InJira
        : RowState.NotInJira;

    /// <summary>The rows the day should have once saved: copies of the kept saved rows (as edited), then the filled-in new rows.</summary>
    public IReadOnlyList<TimeEntry> RowsToSave =>
        Saved.Where(s => !Work.IsRemoved(s)).Select(s => Work.View(s).Copy())
            .Concat(Work.Drafts.Where(d => !Work.IsBlank(d)))
            .ToList();

    /// <summary>Hours the day will have once saved.</summary>
    public TimeSpan Total => TimeEntry.Total(RowsToSave);

    /// <summary>Hours on one issue once saved.</summary>
    public TimeSpan TotalFor(string issueKey) => TimeEntry.Total(RowsToSave.Where(e => e.IssueKey == issueKey));

    public bool HasChanges => HasChangesCounting(suggestions: true);

    /// <summary>
    /// Changes the user made: like <see cref="HasChanges"/> but without untouched suggestions, which are cheap
    /// to make again and not worth a "leave without saving?" question.
    /// </summary>
    public bool HasOwnChanges => HasChangesCounting(suggestions: false);

    /// <summary>Hours an issue will have once saved, when the day's changes alter them; otherwise null.</summary>
    public TimeSpan? UnsavedFor(string issueKey)
    {
        if (!HasChanges) return null;
        var after = TotalFor(issueKey);
        return after == TimeEntry.Total(Saved.Where(e => e.IssueKey == issueKey)) ? null : after;
    }

    /// <summary>True when the only new rows are untouched suggestions and no saved row was changed or removed.</summary>
    public bool HasOnlySuggestions => Work.HasOnlySuggestedDrafts && !Saved.Any(Work.IsChanged);

    private bool HasChangesCounting(bool suggestions) =>
        Work.Drafts.Any(d => !Work.IsBlank(d) && (suggestions || !Work.IsSuggested(d))) || Work.BadHourRows.Count > 0
        || Saved.Any(s => Work.IsRemoved(s) || Work.IsChanged(s));

    /// <summary>What saving the day would change in Jira, and whether it can be saved (see <see cref="DaySave.Errors"/>).</summary>
    public DaySave Plan() => DaySave.Plan(Date, RowsToSave, Saved, Work.BadHourRows);

    /// <summary>Why rows can't be saved, by row id (<see cref="DaySave.DayError"/> for the whole day); empty when the day can be saved.</summary>
    public IReadOnlyDictionary<string, string> Errors => Plan().Errors;

    /// <summary>The hours text typed on a row that could not be read; null when the row's hours are fine.</summary>
    public string? UnreadHours(string rowId) => Work.UnreadHours(rowId);

    /// <summary>The row on an issue as it stands (saved rows as edited, removed ones skipped); null when there is none.</summary>
    public TimeEntry? RowOn(string issueKey)
    {
        var kept = Saved.Where(s => !Work.IsRemoved(s)).Select(Work.View);
        return kept.Concat(Work.Drafts).FirstOrDefault(e => string.Equals(e.IssueKey, issueKey, StringComparison.OrdinalIgnoreCase));
    }

    // ---- Edits. A row is the entry the editor shows (Rows); a saved row's edits go to a copy. ----

    public TimeEntry PickIssue(TimeEntry row, JiraIssue issue) => Changing.PickIssue(row, SavedOf(row), issue);
    public void SetHours(TimeEntry row, string? text) => Changing.SetHours(row, SavedOf(row), text);
    public void SetDescription(TimeEntry row, string? text) => Changing.SetDescription(row, SavedOf(row), text);
    public void Remove(TimeEntry row) => Changing.Remove(row, SavedOf(row));

    /// <summary>Keeps a saved row that was marked removed.</summary>
    public void Restore(TimeEntry row)
    {
        if (SavedOf(row) is { } saved) Changing.Restore(saved);
    }

    public TimeEntry AddRow(JiraIssue? issue = null) => Changing.AddRow(issue);

    private TimeEntry? SavedOf(TimeEntry row) => Saved.FirstOrDefault(s => s.Id == row.Id);

    /// <summary>Adds new rows for the hours still missing of <see cref="Expected"/>, on a best-guess sprint issue.</summary>
    public void FillRest()
    {
        var current = RowsToSave.Where(e => e.Duration > TimeSpan.Zero).ToList();
        var day = new DayInfo(Date, DayKind.Workday, null) { Hours = Expected };
        Changing.AddDrafts(DayFiller.Propose(day, current, _sheet.Sprint));
    }

    /// <summary>
    /// Adds the issues of an earlier day, scaled to the hours still missing of <see cref="Expected"/> (or as
    /// they were when nothing is missing). Untouched suggestions are replaced by the copy.
    /// </summary>
    public void CopyDay(IReadOnlyList<TimeEntry> previous)
    {
        Changing.DropSuggestions();
        var missing = Expected - Total;
        Changing.AddDrafts(DayFiller.Copy(Date, previous, missing > TimeSpan.Zero ? missing : null));
    }

    /// <summary>Suggests rows for the missing hours, unless the day already has changes or new rows.</summary>
    internal void Suggest()
    {
        if (Info is null || HasChanges || Work.Drafts.Count > 0) return;
        var drafts = DayFiller.Propose(Info, Saved, _sheet.Sprint, PreviousWorkedDay?.Rows);
        if (drafts.Count > 0) Changing.AddSuggestions(drafts);
    }

    /// <summary>The whole day was taken off: every saved row is marked removed and suggestions go. Rows the user added stay.</summary>
    internal void TakeOff()
    {
        var work = Changing;
        work.DropSuggestions();
        foreach (var saved in Saved) work.Remove(saved, saved);
    }

    /// <summary>The day off was undone: saved rows marked removed come back.</summary>
    internal void UndoTakeOff() => Work.RestoreAll();
}
