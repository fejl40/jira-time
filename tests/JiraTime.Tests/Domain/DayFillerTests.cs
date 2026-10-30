using JiraTime.Domain;
using static JiraTime.Tests.TestEntries;

namespace JiraTime.Tests;

public class DayFillerTests
{
    private static readonly DateOnly Monday = new(2026, 10, 19);
    private static readonly DayInfo Workday = new(Monday, DayKind.Workday, null);

    private static readonly IssueWithStatus Todo = new("ABC-2", "Fix the build", "Task", "To Do", "new");
    private static readonly IssueWithStatus Doing = new("ABC-1", "Login page", "Story", "In Progress", "indeterminate");

    /// <summary>Work Jira has on Monday, last saved at 10:<paramref name="minute"/>.</summary>
    private static TimeEntry SavedAt(int minute, string key, double hours)
    {
        var entry = Saved(key, hours, Monday);
        entry.UpdatedUtc = new DateTime(2026, 10, 19, 10, minute, 0, DateTimeKind.Utc);
        return entry;
    }

    private static string Show(IEnumerable<TimeEntry> drafts) =>
        string.Join(", ", drafts.Select(d => $"{d.IssueKey} {d.Duration.TotalHours}h"));

    [Fact]
    public void Empty_day_gets_8_hours_on_the_in_progress_issue()
    {
        var drafts = DayFiller.Propose(Workday, [], [Todo, Doing]);

        Assert.Equal("ABC-1 8h", Show(drafts));
        Assert.Equal(Monday, drafts[0].Date);
        Assert.Equal("Login page", drafts[0].IssueSummary);
    }

    [Fact]
    public void The_rest_of_a_day_goes_to_the_issue_last_worked_on_that_day()
    {
        var logged = new[] { SavedAt(5, "XYZ-9", 2), SavedAt(30, "XYZ-7", 1.5), SavedAt(10, "XYZ-8", 1) };

        Assert.Equal("XYZ-7 3.5h", Show(DayFiller.Propose(Workday, logged, [Doing])));
    }

    [Fact]
    public void Rows_not_saved_yet_are_the_latest_work_of_the_day()
    {
        var unsaved = new[] { Draft("NEW-1", 1, Monday), Draft("NEW-2", 1, Monday) };
        var logged = new[] { SavedAt(30, "XYZ-7", 1.5), unsaved[0], unsaved[1], SavedAt(10, "XYZ-8", 1) };

        Assert.Equal("NEW-2", DayFiller.GuessIssue(logged, [Doing])?.Key);
    }

    [Fact]
    public void Without_in_progress_issues_any_sprint_issue_is_used()
    {
        Assert.Equal("ABC-2 8h", Show(DayFiller.Propose(Workday, [], [Todo])));
    }

    [Fact]
    public void Without_any_guess_the_issue_is_left_empty()
    {
        var draft = Assert.Single(DayFiller.Propose(Workday, [], []));

        Assert.Equal("", draft.IssueKey);
        Assert.Equal(TimeSpan.FromHours(8), draft.Duration);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9.5)]
    public void A_full_day_gets_nothing(double logged)
    {
        Assert.Empty(DayFiller.Propose(Workday, [Saved("XYZ-1", logged)], [Doing]));
    }

    [Theory]
    [InlineData(DayKind.Weekend)]
    [InlineData(DayKind.Holiday)]
    [InlineData(DayKind.DayOff)]
    public void Non_workdays_get_nothing(DayKind kind)
    {
        Assert.Empty(DayFiller.Propose(new DayInfo(Monday, kind, null), [], [Doing]));
    }

    [Fact]
    public void An_empty_day_copies_the_previous_worked_day_scaled_to_the_missing_hours()
    {
        var friday = new DateOnly(2026, 10, 16);
        var previous = new[] { Saved("XYZ-1", 6, friday), Saved("XYZ-2", 1.5, friday), Saved("XYZ-1", 0.5, friday) };
        var shortDay = Workday with { Hours = TimeSpan.FromHours(4) };

        Assert.Equal("XYZ-1 6.5h, XYZ-2 1.5h", Show(DayFiller.Propose(Workday, [], [Doing], previous)));
        Assert.Equal("XYZ-1 3.25h, XYZ-2 0.75h", Show(DayFiller.Propose(shortDay, [], [Doing], previous)));
    }

    [Fact]
    public void A_partly_logged_day_still_gets_the_rest_on_the_best_guess()
    {
        var previous = new[] { Saved("XYZ-1", 8, new DateOnly(2026, 10, 16)) };

        Assert.Equal("ABC-5 6h", Show(DayFiller.Propose(Workday, [Saved("ABC-5", 2)], [Doing], previous)));
    }

    [Theory]
    [InlineData(8, "XYZ-1 2.5h, XYZ-2 2.75h, XYZ-3 2.75h")]
    [InlineData(1, "XYZ-1 0.5h, XYZ-2 0.25h, XYZ-3 0.25h")]
    [InlineData(0.25, "XYZ-1 0.25h")]
    public void Copies_are_in_quarter_hours_and_add_up_exactly(double target, string expected)
    {
        var source = new[] { Saved("XYZ-1", 1), Saved("XYZ-2", 1), Saved("XYZ-3", 1) };

        var copy = DayFiller.Copy(Monday.AddDays(1), source, TimeSpan.FromHours(target));

        Assert.Equal(expected, Show(copy));
        Assert.Equal(TimeSpan.FromHours(target), TimeEntry.Total(copy));
        Assert.All(copy, e => Assert.Equal(Monday.AddDays(1), e.Date));
    }

    [Fact]
    public void A_copy_without_target_keeps_the_hours_and_drops_comments_and_empty_rows()
    {
        var source = new[] { Saved("XYZ-1", 2), Saved("", 3), Saved("XYZ-2", 0) };
        source[0].Comment = "Standup";

        var copy = DayFiller.Copy(Monday, source);

        Assert.Equal("XYZ-1 2h", Show(copy));
        Assert.Null(copy[0].Comment);
        Assert.NotEqual(source[0].Id, copy[0].Id);
    }

    [Fact]
    public void The_previous_worked_day_is_found_within_two_weeks()
    {
        var thursday = new DateOnly(2026, 10, 15);
        var rows = new Dictionary<DateOnly, IReadOnlyList<TimeEntry>> { [thursday] = [Saved("XYZ-1", 8, thursday)] };
        IReadOnlyList<TimeEntry> On(DateOnly d) => rows.GetValueOrDefault(d) ?? [];

        Assert.Equal(thursday, DayFiller.PreviousWorkedDay(Monday, On)?.Date);
        Assert.Null(DayFiller.PreviousWorkedDay(thursday.AddDays(DayFiller.LookBackDays + 1), On));
    }
}
