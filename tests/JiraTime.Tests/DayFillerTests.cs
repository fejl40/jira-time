using JiraTime.Domain;

namespace JiraTime.Tests;

public class DayFillerTests
{
    private static readonly DateOnly Monday = new(2026, 10, 19);
    private static readonly DayInfo Workday = new(Monday, DayKind.Workday, null);

    private static readonly SprintIssue Todo = new("ABC-2", "Fix the build", "Task", "To Do", "new");
    private static readonly SprintIssue Doing = new("ABC-1", "Login page", "Story", "In Progress", "indeterminate");

    private static TimeEntry E(string key, double hours, DateOnly? date = null, int updatedMinute = 0) => new()
    {
        IssueKey = key, IssueSummary = key + " summary", Date = date ?? Monday, Duration = TimeSpan.FromHours(hours),
        UpdatedUtc = new DateTime(2026, 10, 19, 10, updatedMinute, 0, DateTimeKind.Utc),
    };

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
        var logged = new[] { E("XYZ-9", 2, updatedMinute: 5), E("XYZ-7", 1.5, updatedMinute: 30), E("XYZ-8", 1, updatedMinute: 10) };

        Assert.Equal("XYZ-7 3.5h", Show(DayFiller.Propose(Workday, logged, [Doing])));
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
        Assert.Empty(DayFiller.Propose(Workday, [E("XYZ-1", logged)], [Doing]));
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
    public void Period_fill_covers_short_workdays_up_to_today_only()
    {
        // Thu 15 Oct .. Wed 21 Oct, today Tue 20 Oct. Fri 16 is full, Mon 19 has 2 h, Sat/Sun are weekend.
        var start = new DateOnly(2026, 10, 15);
        var days = Enumerable.Range(0, 7).Select(i => start.AddDays(i)).Select(d => new DayInfo(d,
            d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? DayKind.Weekend : DayKind.Workday, null)).ToList();
        var logged = new[] { E("XYZ-1", 8, new DateOnly(2026, 10, 16)), E("XYZ-2", 2) };

        var plan = DayFiller.ProposePeriod(days, today: new DateOnly(2026, 10, 20), logged, [Doing]);

        Assert.Equal(new[] { "2026-10-15", "2026-10-19", "2026-10-20" }.Select(DateOnly.Parse), plan.Keys.Order());
        Assert.Equal("ABC-1 8h", Show(plan[new DateOnly(2026, 10, 15)])); // nothing before it to copy
        Assert.Equal("XYZ-2 6h", Show(plan[Monday]));                      // partly logged: the rest on the last issue
        Assert.Equal("XYZ-2 8h", Show(plan[new DateOnly(2026, 10, 20)])); // copies Monday as filled
        Assert.All(plan, p => Assert.All(p.Value, d => Assert.Equal(p.Key, d.Date)));
    }

    [Fact]
    public void An_empty_day_copies_the_previous_worked_day_scaled_to_the_missing_hours()
    {
        var friday = new DateOnly(2026, 10, 16);
        var previous = new[] { E("XYZ-1", 6, friday), E("XYZ-2", 1.5, friday), E("XYZ-1", 0.5, friday) };
        var shortDay = Workday with { Hours = TimeSpan.FromHours(4) };

        Assert.Equal("XYZ-1 6.5h, XYZ-2 1.5h", Show(DayFiller.Propose(Workday, [], [Doing], previous)));
        Assert.Equal("XYZ-1 3.25h, XYZ-2 0.75h", Show(DayFiller.Propose(shortDay, [], [Doing], previous)));
    }

    [Fact]
    public void A_partly_logged_day_still_gets_the_rest_on_the_best_guess()
    {
        var previous = new[] { E("XYZ-1", 8, new DateOnly(2026, 10, 16)) };

        Assert.Equal("ABC-5 6h", Show(DayFiller.Propose(Workday, [E("ABC-5", 2)], [Doing], previous)));
    }

    [Theory]
    [InlineData(8, "XYZ-1 2.5h, XYZ-2 2.75h, XYZ-3 2.75h")]
    [InlineData(1, "XYZ-1 0.5h, XYZ-2 0.25h, XYZ-3 0.25h")]
    [InlineData(0.25, "XYZ-1 0.25h")]
    public void Copies_are_in_quarter_hours_and_add_up_exactly(double target, string expected)
    {
        var source = new[] { E("XYZ-1", 1), E("XYZ-2", 1), E("XYZ-3", 1) };

        var copy = DayFiller.Copy(Monday.AddDays(1), source, TimeSpan.FromHours(target));

        Assert.Equal(expected, Show(copy));
        Assert.Equal(TimeSpan.FromHours(target), copy.Aggregate(TimeSpan.Zero, (t, e) => t + e.Duration));
        Assert.All(copy, e => Assert.Equal(Monday.AddDays(1), e.Date));
    }

    [Fact]
    public void A_copy_without_target_keeps_the_hours_and_drops_comments_and_empty_rows()
    {
        var source = new[] { E("XYZ-1", 2), E("", 3), E("XYZ-2", 0) };
        source[0].Description = "Standup";

        var copy = DayFiller.Copy(Monday, source);

        Assert.Equal("XYZ-1 2h", Show(copy));
        Assert.Null(copy[0].Description);
        Assert.NotEqual(source[0].Id, copy[0].Id);
    }

    [Fact]
    public void The_previous_worked_day_is_found_within_two_weeks()
    {
        var thursday = new DateOnly(2026, 10, 15);
        var rows = new Dictionary<DateOnly, IReadOnlyList<TimeEntry>> { [thursday] = [E("XYZ-1", 8, thursday)] };
        IReadOnlyList<TimeEntry> On(DateOnly d) => rows.GetValueOrDefault(d) ?? [];

        Assert.Equal(thursday, DayFiller.PreviousWorkedDay(Monday, On)?.Date);
        Assert.Null(DayFiller.PreviousWorkedDay(thursday.AddDays(DayFiller.LookBackDays + 1), On));
    }
}
