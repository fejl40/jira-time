namespace JiraTime.Domain;

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

    public bool HasChanges(IReadOnlyList<TimeEntry> saved) =>
        Drafts.Any(d => !IsBlank(d)) || BadHours.Count > 0
        || saved.Any(s => Removed.Contains(s.Id) || IsChanged(s));

    /// <summary>
    /// Changes the user made: like <see cref="HasChanges"/> but without untouched suggestions, which
    /// are cheap to make again and not worth a "leave without saving?" question.
    /// </summary>
    public bool HasOwnChanges(IReadOnlyList<TimeEntry> saved) =>
        Drafts.Any(d => !IsBlank(d) && !Suggested.Contains(d.Id)) || BadHours.Count > 0
        || saved.Any(s => Removed.Contains(s.Id) || IsChanged(s));

    /// <summary>The user changed a new row; a suggestion they edited counts as their own row.</summary>
    public void Touched(TimeEntry row) => Suggested.Remove(row.Id);

    /// <summary>The rows the day should have once saved: kept saved rows (as edited) and filled-in new rows.</summary>
    public List<TimeEntry> RowsToSave(IReadOnlyList<TimeEntry> saved) =>
        saved.Where(s => !Removed.Contains(s.Id)).Select(s => View(s).Copy())
            .Concat(Drafts.Where(d => !IsBlank(d)))
            .ToList();

    /// <summary>Hours the day will have once saved.</summary>
    public TimeSpan Total(IReadOnlyList<TimeEntry> saved) =>
        RowsToSave(saved).Aggregate(TimeSpan.Zero, (t, e) => t + e.Duration);

    /// <summary>Hours on one issue once saved.</summary>
    public TimeSpan TotalFor(IReadOnlyList<TimeEntry> saved, string issueKey) =>
        RowsToSave(saved).Where(e => e.IssueKey == issueKey).Aggregate(TimeSpan.Zero, (t, e) => t + e.Duration);

    /// <summary>Drops every unsaved change.</summary>
    public void Clear()
    {
        _edits.Clear();
        Removed.Clear();
        Drafts.Clear();
        Suggested.Clear();
        BadHours.Clear();
    }
}
