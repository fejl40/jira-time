using JiraTime.Domain;

namespace JiraTime.Tests;

/// <summary>The change list Save all shows: added, changed and removed rows per day, and the totals.</summary>
public class UnsavedChangesTests
{
    private static readonly Period Period = Period.For(new DateOnly(2026, 10, 20));
    private static readonly DateOnly Friday = new(2026, 10, 16), Monday = new(2026, 10, 19), Tuesday = new(2026, 10, 20);

    private static TimeEntry Saved(string key, double hours, DateOnly date, string? worklog = "1", string? comment = null) => new()
    {
        IssueKey = key, IssueSummary = key + " summary", Date = date, Duration = TimeSpan.FromHours(hours),
        Description = comment, JiraWorklogId = worklog, SyncPending = worklog is null,
    };

    private static TimeEntry Draft(string key, double hours, DateOnly date) =>
        new() { IssueKey = key, IssueSummary = key + " summary", Date = date, Duration = TimeSpan.FromHours(hours) };

    private static PeriodSheet Sheet(params TimeEntry[] entries)
    {
        var sheet = new PeriodSheet(Period, () => Tuesday);
        var days = Period.Days.ToDictionary(d => d, d => DayInfo.For(d, new Dictionary<DateOnly, DayOff>()));
        sheet.Load(entries, [], days);
        return sheet;
    }

    [Fact]
    public void Each_unsaved_day_lists_its_added_changed_and_removed_rows_with_hours()
    {
        var moved = Saved("ABC-1", 4, Friday, comment: "Review");
        var gone = Saved("ABC-2", 2, Friday);
        var same = Saved("ABC-3", 2, Friday);
        var sheet = Sheet(moved, gone, same);
        var friday = sheet.WorkFor(Friday);
        friday.PickIssue(moved, moved, new JiraIssue("ABC-9", "Other"));
        friday.SetHours(moved, moved, "5");
        friday.Remove(gone, gone);
        sheet.WorkFor(Monday).Drafts.Add(Draft("ABC-4", 8, Monday));

        var changes = UnsavedChanges.Of(sheet);

        Assert.Equal([Friday, Monday], changes.Days.Select(d => d.Day));
        var rows = changes.Days[0].Rows;
        Assert.Equal([RowChangeKind.Changed, RowChangeKind.Removed], rows.Select(r => r.Kind)); // saved order; the unchanged row isn't listed
        var changed = rows[0];
        Assert.Equal(("ABC-9", "ABC-1", TimeSpan.FromHours(5), TimeSpan.FromHours(4)), (changed.IssueKey, changed.IssueBefore, changed.Hours, changed.HoursBefore));
        Assert.False(changed.CommentChanged);
        Assert.True(rows[1].DeletesWorklog);
        Assert.Equal((TimeSpan.FromHours(8), TimeSpan.FromHours(7)), (changes.Days[0].Before, changes.Days[0].After));
        Assert.Equal(RowChangeKind.Added, Assert.Single(changes.Days[1].Rows).Kind);
    }

    [Fact]
    public void Totals_count_the_rows_hours_and_deleted_worklogs_of_the_days_to_save()
    {
        var gone = Saved("ABC-2", 2, Friday);
        var waiting = Saved("ABC-3", 1, Friday, worklog: null); // not in Jira yet: removing it deletes no worklog
        var sheet = Sheet(gone, waiting);
        sheet.WorkFor(Friday).Remove(gone, gone);
        sheet.WorkFor(Friday).Remove(waiting, waiting);
        sheet.WorkFor(Monday).Drafts.Add(Draft("ABC-4", 8, Monday));

        var changes = UnsavedChanges.Of(sheet);

        Assert.Equal(2, changes.Count(RowChangeKind.Removed));
        Assert.Equal(1, changes.Count(RowChangeKind.Added));
        Assert.Equal(1, changes.DeletedWorklogs);
        Assert.Equal((TimeSpan.FromHours(3), TimeSpan.FromHours(8)), (changes.Before, changes.After));
    }

    [Fact]
    public void A_day_that_cannot_be_saved_says_why_and_is_left_out_of_the_totals()
    {
        var sheet = Sheet();
        var monday = sheet.WorkFor(Monday);
        monday.Drafts.Add(Draft("", 2, Monday));
        var tuesday = sheet.WorkFor(Tuesday);
        var row = tuesday.AddRow(new JiraIssue("ABC-1", "Login"));
        tuesday.SetHours(row, null, "lots");

        var changes = UnsavedChanges.Of(sheet);

        Assert.Equal("Pick a Jira issue.", changes.Days[0].Problem);
        Assert.Equal(DayChanges.BadHours, changes.Days[1].Problem);
        Assert.Empty(changes.ToSave);
        Assert.Equal(TimeSpan.Zero, changes.After);
    }

    [Fact]
    public void A_changed_comment_is_noted_and_blank_new_rows_are_ignored()
    {
        var entry = Saved("ABC-1", 4, Friday, comment: "Review");
        var sheet = Sheet(entry);
        var friday = sheet.WorkFor(Friday);
        friday.SetDescription(entry, entry, "Planning");
        friday.AddRow();

        var change = Assert.Single(UnsavedChanges.Of(sheet).Days.Single().Rows);

        Assert.True(change.CommentChanged);
        Assert.Equal("Planning", change.Comment);
        Assert.Null(change.HoursBefore);
        Assert.Null(change.IssueBefore);
    }
}
