namespace JiraTime.Domain;

/// <summary>
/// The unsaved changes to one day, kept while the period is open: edited copies of saved rows, saved rows
/// marked for removal, new rows (suggested or added) and hours text that could not be read. It knows
/// single entries only; <see cref="SheetDay"/> lays it over the day's saved rows.
/// </summary>
public sealed class DayWork(DateOnly date)
{
    private readonly Dictionary<string, TimeEntry> _edits = [];
    /// <summary>Ids of saved entries the user removed; they are deleted (with their worklogs) on save.</summary>
    private readonly HashSet<string> _removed = [];
    private readonly List<TimeEntry> _drafts = [];
    /// <summary>Ids of drafts that came from a suggestion.</summary>
    private readonly HashSet<string> _suggested = [];
    /// <summary>Hours text that could not be read, by row id.</summary>
    private readonly Dictionary<string, string> _badHours = [];

    public DateOnly Date { get; } = date;

    /// <summary>New rows: suggestions and added issues, in the order they came.</summary>
    public IReadOnlyList<TimeEntry> Drafts => _drafts.AsReadOnly();

    /// <summary>Ids of rows whose typed hours could not be read. A day with any can't be saved.</summary>
    public IReadOnlyCollection<string> BadHourRows => _badHours.Keys;

    /// <summary>The hours text typed on a row that could not be read; null when the row's hours are fine.</summary>
    public string? UnreadHours(string rowId) => _badHours.GetValueOrDefault(rowId);

    /// <summary>The saved entry as edited so far (the entry itself when untouched).</summary>
    public TimeEntry View(TimeEntry saved) => _edits.GetValueOrDefault(saved.Id) ?? saved;

    /// <summary>True when the saved entry was edited to other values (removing it doesn't count).</summary>
    public bool IsChanged(TimeEntry saved) => _edits.TryGetValue(saved.Id, out var copy) && Differs(saved, copy);

    public bool IsRemoved(TimeEntry saved) => _removed.Contains(saved.Id);

    public bool IsSuggested(TimeEntry draft) => _suggested.Contains(draft.Id);

    /// <summary>A new row nobody filled in (no hours, no comment). Saving skips it.</summary>
    public bool IsBlank(TimeEntry draft) =>
        draft.Duration == TimeSpan.Zero && string.IsNullOrWhiteSpace(draft.Description) && !_badHours.ContainsKey(draft.Id);

    /// <summary>True when every new row is an untouched suggestion and no saved row is marked removed.</summary>
    public bool HasOnlySuggestedDrafts => _drafts.All(IsSuggested) && _removed.Count == 0;

    private static bool Differs(TimeEntry a, TimeEntry b) =>
        a.IssueKey != b.IssueKey || a.Duration != b.Duration || (a.Description ?? "") != (b.Description ?? "");

    /// <summary>
    /// The entry to change for a row: a copy of the saved entry, made on first use so the saved entry stays as
    /// stored, or the new row itself, which then no longer counts as a suggestion.
    /// </summary>
    private TimeEntry Editable(TimeEntry row, TimeEntry? saved)
    {
        if (saved is not null) return _edits.TryGetValue(saved.Id, out var copy) ? copy : _edits[saved.Id] = saved.Copy();
        _suggested.Remove(row.Id);
        return row;
    }

    /// <summary>Puts the row on another issue. Returns the entry that changed.</summary>
    /// <param name="saved">The stored entry of a saved row; null for a new row.</param>
    public TimeEntry PickIssue(TimeEntry row, TimeEntry? saved, JiraIssue issue)
    {
        var e = Editable(row, saved);
        e.IssueKey = issue.Key;
        e.IssueSummary = issue.Summary;
        return e;
    }

    /// <summary>Sets the row's hours from what was typed. Text that can't be read is kept (see <see cref="UnreadHours"/>) and the hours become zero.</summary>
    public void SetHours(TimeEntry row, TimeEntry? saved, string? text)
    {
        var e = Editable(row, saved);
        text ??= "";
        var hours = TypedHours.Parse(text);
        if (hours is null && text.Trim() != "") _badHours[e.Id] = text;
        else _badHours.Remove(e.Id);
        e.Duration = hours ?? TimeSpan.Zero;
    }

    /// <summary>Sets the row's comment, trimmed; an empty comment becomes none.</summary>
    public void SetDescription(TimeEntry row, TimeEntry? saved, string? text)
    {
        text = text?.Trim();
        Editable(row, saved).Description = string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>A saved row is marked removed (deleted on save); a new row is dropped right away.</summary>
    public void Remove(TimeEntry row, TimeEntry? saved)
    {
        if (saved is not null)
        {
            _removed.Add(saved.Id);
            return;
        }
        _drafts.Remove(row);
        _suggested.Remove(row.Id);
        _badHours.Remove(row.Id);
    }

    /// <summary>Keeps a saved row that was marked removed.</summary>
    public void Restore(TimeEntry saved) => _removed.Remove(saved.Id);

    /// <summary>Keeps every saved row that was marked removed.</summary>
    public void RestoreAll() => _removed.Clear();

    /// <summary>Adds an empty new row, optionally on an issue, and returns it.</summary>
    public TimeEntry AddRow(JiraIssue? issue = null)
    {
        var draft = new TimeEntry { IssueKey = issue?.Key ?? "", IssueSummary = issue?.Summary ?? "", Date = Date };
        _drafts.Add(draft);
        return draft;
    }

    /// <summary>Adds new rows that are the user's own from the start (filled or copied on request).</summary>
    public void AddDrafts(IEnumerable<TimeEntry> drafts) => _drafts.AddRange(drafts);

    /// <summary>Adds suggested rows; they count as suggestions until the user changes them.</summary>
    public void AddSuggestions(IEnumerable<TimeEntry> drafts)
    {
        foreach (var draft in drafts)
        {
            _drafts.Add(draft);
            _suggested.Add(draft.Id);
        }
    }

    /// <summary>Drops the untouched suggestions; rows the user added or changed stay.</summary>
    public void DropSuggestions()
    {
        _drafts.RemoveAll(IsSuggested);
        _suggested.Clear();
    }
}
