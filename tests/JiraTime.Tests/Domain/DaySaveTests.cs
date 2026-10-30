using JiraTime.Domain;
using static JiraTime.Tests.Support.TestEntries;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Domain;

/// <summary>Planning a day's save: whether it can be saved, what is removed, sent and checked, always on copies.</summary>
public class DaySaveTests
{
    private static readonly DateOnly Day = new(2026, 10, 19);

    /// <summary>Work as it was read from Jira worklog 10001, with <paramref name="id"/> to tell the copies apart.</summary>
    private static TimeEntry Read(string id = "jira-10001")
    {
        var read = Saved("ABC-1", 2, Day, "Review", worklog: "10001");
        read.Id = id;
        return read;
    }

    [Fact]
    public void Any_error_leaves_nothing_to_do()
    {
        var noIssue = Row("", 1);

        var plan = DaySave.Plan(Day, [noIssue, Row("ABC-1", 20), Row("ABC-2", 5)], [Read()]);

        Assert.Equal([DaySave.DayError, noIssue.Id], plan.Errors.Keys.Order());
        Assert.Equal(TimeEntry.TooManyHours, plan.Errors[DaySave.DayError]);
        Assert.Empty(plan.Removed);
        Assert.Empty(plan.ToCheck);
        Assert.Empty(plan.RowsToSend(DateTime.UnixEpoch));
    }

    [Fact]
    public void Hours_that_cannot_be_read_are_an_error_on_their_row()
    {
        var unreadable = Row("ABC-1", 0);

        var plan = DaySave.Plan(Day, [Row("ABC-2", 1), unreadable], [], badHours: [unreadable.Id]);

        Assert.Equal(DaySave.BadHours, Assert.Single(plan.Errors).Value);
        Assert.Equal(unreadable.Id, plan.Errors.Keys.Single());
        Assert.Equal(Day, plan.Date);
    }

    [Fact]
    public void Moving_hours_between_rows_near_24_hours_is_allowed()
    {
        Assert.Empty(DaySave.Plan(Day, [Row("ABC-1", 16), Row("ABC-2", 8)], []).Errors);
    }

    [Fact]
    public void Missing_work_is_removed_new_and_changed_rows_are_sent_and_what_Jira_has_is_checked()
    {
        var kept = Read("kept");
        var changed = Read("changed");
        var removed = Read("removed");
        var removedNotInJira = Draft("ABC-9", 1, Day);
        removedNotInJira.Id = "local";
        var edited = changed.Copy();
        edited.Duration = TimeSpan.FromHours(3);
        var added = Row("ABC-4", 1);

        var plan = DaySave.Plan(Day, [kept.Copy(), edited, added], [kept, changed, removed, removedNotInJira]);

        Assert.Empty(plan.Errors);
        Assert.Equal(["removed", "local"], plan.Removed.Select(e => e.Id));
        Assert.Equal(["removed", "changed"], plan.ToCheck.Select(e => e.Id));
        Assert.Equal([edited.Id, added.Id], plan.RowsToSend(DateTime.UnixEpoch).Select(e => e.Id));
    }

    [Fact]
    public void Rows_to_send_are_put_on_the_day_and_take_Jira_bookkeeping_from_what_was_read_and_the_time_given()
    {
        var read = Read();
        read.CreatedUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var edited = read.Copy();
        edited.Duration = TimeSpan.FromHours(3);
        edited.LeftOldIssue(); // the copy's worklog differs from what was read
        edited.Date = Day.AddDays(1);
        var now = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

        var row = Assert.Single(DaySave.Plan(Day, [edited], [read]).RowsToSend(now));

        Assert.Equal(("10001", "ABC-1", read.CreatedUtc, now), (row.JiraWorklogId, row.JiraWorklogIssueKey, row.CreatedUtc, row.UpdatedUtc));
        Assert.Equal(Day, row.Date);
    }

    [Fact]
    public void Planning_and_sending_work_on_copies()
    {
        var read = Read();
        var gone = Read("gone");
        var edited = read.Copy();
        edited.Duration = TimeSpan.FromHours(3);
        var added = Row("ABC-2", 1);
        added.Date = Day.AddDays(1);
        var rows = new[] { edited, added };
        var before = string.Join(",", rows.Concat([read, gone]).Select(e => $"{e.Id} {e.Date} {e.Duration} {e.JiraWorklogId} {e.UpdatedUtc:O}"));

        var plan = DaySave.Plan(Day, rows, [read, gone]);
        var sent = plan.RowsToSend(DateTime.UnixEpoch);
        sent[0].Sent("changed by the writer");
        plan.Removed[0].MarkRemovalPending();

        Assert.Equal(before, string.Join(",", rows.Concat([read, gone]).Select(e => $"{e.Id} {e.Date} {e.Duration} {e.JiraWorklogId} {e.UpdatedUtc:O}")));
        Assert.False(gone.DeletePending);
        Assert.Equal("10001", plan.RowsToSend(DateTime.UnixEpoch)[0].JiraWorklogId); // each call gives fresh copies
    }

    [Fact]
    public void Save_all_plans_every_unsaved_day_of_the_sheet_in_order()
    {
        var sheet = TestSheets.For(Day);
        var tuesday = sheet.Day(Day.AddDays(1));
        tuesday.SetHours(tuesday.AddRow(new JiraIssue("ABC-1", "")), "8");
        var monday = sheet.Day(Day);
        monday.SetHours(monday.AddRow(), "8");

        var plans = sheet.PlanSaveAll();

        Assert.Equal([Day, Day.AddDays(1)], plans.Select(p => p.Date));
        Assert.Equal("Pick a Jira issue.", Assert.Single(plans[0].Errors).Value);
        Assert.Empty(plans[1].Errors);
    }
}
