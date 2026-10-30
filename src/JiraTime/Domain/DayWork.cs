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
/// Unsaved changes to one day: edited copies of saved rows, saved rows marked for removal, and new
/// rows. Saved entries are passed in on each call, so the work stays valid while they reload.
/// </summary>
public sealed class DayWork(DateOnly date)
{
    private readonly Dictionary<string, TimeEntry> _edits = [];

    public DateOnly Date { get; } = date;

    /// <summary>Ids of saved entries the user removed; they are deleted (with their worklogs) on save.</summary>
    public HashSet<string> Removed { get; } = [];

    /// <summary>New rows: suggestions and added issues.</summary>
    public List<TimeEntry> Drafts { get; } = [];

    /// <summary>Ids of drafts that came from a suggestion.</summary>
    public HashSet<string> Suggested { get; } = [];

    /// <summary>Hours text that could not be read, by row id. Such a day can't be saved.</summary>
    public Dictionary<string, string> BadHours { get; } = [];

    /// <summary>The saved entry as edited so far (the entry itself when untouched).</summary>
    public TimeEntry View(TimeEntry saved) => _edits.GetValueOrDefault(saved.Id) ?? saved;

    /// <summary>The copy of a saved entry to change; made on first use, so the saved entry stays as stored.</summary>
    public TimeEntry Edit(TimeEntry saved) =>
        _edits.TryGetValue(saved.Id, out var copy) ? copy : _edits[saved.Id] = saved.Copy();

    public static bool Differs(TimeEntry a, TimeEntry b) =>
        a.IssueKey != b.IssueKey || a.Duration != b.Duration || (a.Description ?? "") != (b.Description ?? "");

    /// <summary>True when the saved entry was edited (not removed).</summary>
    public bool IsChanged(TimeEntry saved) => _edits.TryGetValue(saved.Id, out var copy) && Differs(saved, copy);

    public bool IsRemoved(TimeEntry saved) => Removed.Contains(saved.Id);

    /// <summary>A new row nobody filled in (no hours, no comment). Saving skips it.</summary>
    public bool IsBlank(TimeEntry draft) =>
        draft.Duration == TimeSpan.Zero && string.IsNullOrWhiteSpace(draft.Description) && !BadHours.ContainsKey(draft.Id);

    public bool HasChanges(IReadOnlyList<TimeEntry> saved) => HasChanges(saved, countSuggestions: true);

    /// <summary>
    /// Changes the user made: like <see cref="HasChanges"/> but without untouched suggestions, which
    /// are cheap to make again and not worth a "leave without saving?" question.
    /// </summary>
    public bool HasOwnChanges(IReadOnlyList<TimeEntry> saved) => HasChanges(saved, countSuggestions: false);

    /// <summary>True when the only new rows are untouched suggestions and no saved row was changed or removed.</summary>
    public bool HasOnlySuggestions(IReadOnlyList<TimeEntry> saved) =>
        Drafts.All(d => Suggested.Contains(d.Id)) && !saved.Any(IsChanged) && Removed.Count == 0;

    private bool HasChanges(IReadOnlyList<TimeEntry> saved, bool countSuggestions) =>
        Drafts.Any(d => !IsBlank(d) && (countSuggestions || !Suggested.Contains(d.Id))) || BadHours.Count > 0
        || saved.Any(s => Removed.Contains(s.Id) || IsChanged(s));

    /// <summary>The day's rows as the editor shows them: saved rows (as edited, or removed), then new rows.</summary>
    public IReadOnlyList<WorkRow> Rows(IReadOnlyList<TimeEntry> saved)
    {
        var rows = saved.Select(s => new WorkRow(View(s), s, SavedState(s))).ToList();
        rows.AddRange(Drafts.Select(d => new WorkRow(d, null, Suggested.Contains(d.Id) ? RowState.Suggested : RowState.New)));
        return rows;
    }

    private RowState SavedState(TimeEntry saved) =>
        IsRemoved(saved) ? RowState.Removed
        : IsChanged(saved) ? RowState.Changed
        : View(saved).IsSynced ? RowState.InJira
        : RowState.NotInJira;

    /// <summary>The user changed a new row; a suggestion they edited counts as their own row.</summary>
    public void Touched(TimeEntry row) => Suggested.Remove(row.Id);

    /// <summary>The rows the day should have once saved: kept saved rows (as edited) and filled-in new rows.</summary>
    public List<TimeEntry> RowsToSave(IReadOnlyList<TimeEntry> saved) =>
        saved.Where(s => !Removed.Contains(s.Id)).Select(s => View(s).Copy())
            .Concat(Drafts.Where(d => !IsBlank(d)))
            .ToList();

    /// <summary>Hours the day will have once saved.</summary>
    public TimeSpan Total(IReadOnlyList<TimeEntry> saved) =>
        TimeEntry.Total(RowsToSave(saved));

    /// <summary>Hours on one issue once saved.</summary>
    public TimeSpan TotalFor(IReadOnlyList<TimeEntry> saved, string issueKey) =>
        TimeEntry.Total(RowsToSave(saved).Where(e => e.IssueKey == issueKey));

    /// <summary>
    /// The entry to change for a row: the copy of the saved entry, or the new row itself (which then
    /// no longer counts as a suggestion).
    /// </summary>
    /// <param name="saved">The stored entry for saved rows; null for new rows.</param>
    public TimeEntry Editable(TimeEntry row, TimeEntry? saved)
    {
        if (saved is not null) return Edit(saved);
        Touched(row);
        return row;
    }

    /// <summary>Puts the row on another issue. Returns the entry that changed.</summary>
    public TimeEntry PickIssue(TimeEntry row, TimeEntry? saved, JiraIssue issue)
    {
        var e = Editable(row, saved);
        e.IssueKey = issue.Key;
        e.IssueSummary = issue.Summary;
        return e;
    }

    /// <summary>
    /// Sets the row's hours from what was typed. Text that can't be read is kept in
    /// <see cref="BadHours"/> (so the day editor and Save all can report it) and the hours become zero.
    /// </summary>
    public void SetHours(TimeEntry row, TimeEntry? saved, string? text)
    {
        var e = Editable(row, saved);
        text ??= "";
        var hours = TypedHours.Parse(text);
        if (hours is null && text.Trim() != "") BadHours[e.Id] = text;
        else BadHours.Remove(e.Id);
        e.Duration = hours ?? TimeSpan.Zero;
    }

    /// <summary>Sets the row's comment, trimmed; an empty comment becomes none.</summary>
    public void SetDescription(TimeEntry row, TimeEntry? saved, string? text)
    {
        text = text?.Trim();
        Editable(row, saved).Description = string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>Adds suggested rows; they count as suggestions until the user changes them.</summary>
    public void AddSuggestions(IEnumerable<TimeEntry> drafts)
    {
        foreach (var draft in drafts)
        {
            Drafts.Add(draft);
            Suggested.Add(draft.Id);
        }
    }

    /// <summary>The day's row on an issue as it stands (saved rows as edited, removed ones skipped); null when there is none.</summary>
    public TimeEntry? RowOn(IReadOnlyList<TimeEntry> saved, string issueKey)
    {
        var kept = saved.Where(s => !IsRemoved(s)).Select(View);
        return kept.Concat(Drafts).FirstOrDefault(e => string.Equals(e.IssueKey, issueKey, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Adds an empty new row, optionally on an issue, and returns it.</summary>
    public TimeEntry AddRow(JiraIssue? issue = null)
    {
        var draft = new TimeEntry { IssueKey = issue?.Key ?? "", IssueSummary = issue?.Summary ?? "", Date = Date };
        Drafts.Add(draft);
        return draft;
    }

    /// <summary>Adds new rows for the hours still missing of <paramref name="expected"/>, on a best-guess issue.</summary>
    public void FillRest(IReadOnlyList<TimeEntry> saved, TimeSpan expected, IReadOnlyList<IssueWithStatus> sprint)
    {
        var current = RowsToSave(saved).Where(e => e.Duration > TimeSpan.Zero).ToList();
        var day = new DayInfo(Date, DayKind.Workday, null) { Hours = expected };
        Drafts.AddRange(DayFiller.Propose(day, current, sprint));
    }

    /// <summary>
    /// Adds the issues of an earlier day, scaled to the hours still missing of <paramref name="expected"/>
    /// (or as they were when nothing is missing). Untouched suggestions are replaced by the copy.
    /// </summary>
    public void CopyDay(IReadOnlyList<TimeEntry> saved, TimeSpan expected, IReadOnlyList<TimeEntry> previous)
    {
        Drafts.RemoveAll(d => Suggested.Contains(d.Id));
        Suggested.Clear();
        var missing = expected - Total(saved);
        Drafts.AddRange(DayFiller.Copy(Date, previous, missing > TimeSpan.Zero ? missing : null));
    }

    /// <summary>A saved row is marked removed (deleted on save); a new row is dropped right away.</summary>
    public void Remove(TimeEntry row, TimeEntry? saved)
    {
        if (saved is not null)
        {
            Removed.Add(saved.Id);
            return;
        }
        Drafts.Remove(row);
        Suggested.Remove(row.Id);
        BadHours.Remove(row.Id);
    }

    /// <summary>Keeps a saved row that was marked removed.</summary>
    public void Restore(TimeEntry saved) => Removed.Remove(saved.Id);

    /// <summary>
    /// The whole day was taken off: every saved row is marked removed, so Save all deletes its worklogs,
    /// and suggestions go. Rows the user added stay.
    /// </summary>
    public void TakeOff(IReadOnlyList<TimeEntry> saved)
    {
        Drafts.RemoveAll(d => Suggested.Contains(d.Id));
        Suggested.Clear();
        foreach (var s in saved) Removed.Add(s.Id);
    }
}
