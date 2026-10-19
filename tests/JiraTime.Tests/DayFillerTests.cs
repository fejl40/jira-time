using JiraTime.Domain;

namespace JiraTime.Tests;

public class DayFillerTests
{
    private static readonly DateOnly Monday = new(2026, 10, 19);
    private static readonly DayInfo Workday = new(Monday, DayKind.Workday, null);
    private static readonly Workday Window = Domain.Workday.Default; // 08:00–16:30, lunch 12:00–12:30

    private static readonly SprintIssue Todo = new("ABC-2", "Fix the build", "Task", "To Do", "new");
    private static readonly SprintIssue Doing = new("ABC-1", "Login page", "Story", "In Progress", "indeterminate");

    private static TimeEntry E(string key, string start, string end) =>
        new() { IssueKey = key, IssueSummary = key + " summary", Date = Monday, Start = TimeOnly.Parse(start), End = TimeOnly.Parse(end) };

    private static string Show(IEnumerable<TimeEntry> drafts) =>
        string.Join(", ", drafts.Select(d => $"{d.IssueKey} {d.Start:HH\\:mm}-{d.End:HH\\:mm}"));

    private static TimeSpan Total(IEnumerable<TimeEntry> es) => es.Aggregate(TimeSpan.Zero, (t, e) => t + e.Duration);

    [Fact]
    public void Empty_day_is_filled_around_lunch_with_the_in_progress_issue()
    {
        var drafts = DayFiller.Propose(Workday, Window, [], [Todo, Doing]);

        Assert.Equal("ABC-1 08:00-12:00, ABC-1 12:30-16:30", Show(drafts));
        Assert.Equal(TimeSpan.FromHours(8), Total(drafts));
        Assert.All(drafts, d => Assert.Equal(Monday, d.Date));
        Assert.Equal("Login page", drafts[0].IssueSummary);
    }

    [Fact]
    public void Gaps_take_the_issue_logged_right_before_them()
    {
        var logged = new[] { E("XYZ-9", "08:00", "10:00"), E("XYZ-7", "13:00", "14:00") };

        var drafts = DayFiller.Propose(Workday, Window, logged, [Doing]);

        // 10–12 follows XYZ-9; 12:30–13 precedes XYZ-7 (nothing right before it but lunch, and the
        // 10–12 draft carries XYZ-9 across lunch); 14–16:30 follows XYZ-7.
        Assert.Equal("XYZ-9 10:00-12:00, XYZ-9 12:30-13:00, XYZ-7 14:00-16:30", Show(drafts));
        Assert.Equal(TimeSpan.FromHours(8), Total([.. logged, .. drafts]));
    }

    [Fact]
    public void A_gap_with_nothing_before_it_takes_the_issue_right_after_it()
    {
        var drafts = DayFiller.Propose(Workday, Window, [E("XYZ-9", "09:00", "12:00"), E("XYZ-8", "12:30", "16:30")], [Doing]);

        Assert.Equal("XYZ-9 08:00-09:00", Show(drafts));
    }

    [Fact]
    public void Without_neighbours_or_in_progress_issues_any_sprint_issue_is_used()
    {
        Assert.Equal("ABC-2", DayFiller.Propose(Workday, Window, [], [Todo])[0].IssueKey);
    }

    [Fact]
    public void Without_any_guess_the_issue_is_left_empty()
    {
        var drafts = DayFiller.Propose(Workday, Window, [], []);

        Assert.Equal(2, drafts.Count);
        Assert.All(drafts, d => Assert.Equal("", d.IssueKey));
    }

    [Fact]
    public void Proposals_stop_at_eight_hours_when_time_was_logged_outside_the_window()
    {
        var early = E("XYZ-1", "06:00", "08:00");

        var drafts = DayFiller.Propose(Workday, Window, [early], []);

        // 2 h + 6 h = 8 h; the first gap follows the early entry and carries it across lunch.
        Assert.Equal("XYZ-1 08:00-12:00, XYZ-1 12:30-14:30", Show(drafts));
        Assert.Equal(TimeSpan.FromHours(6), Total(drafts));
    }

    [Fact]
    public void A_full_day_gets_nothing()
    {
        Assert.Empty(DayFiller.Propose(Workday, Window, [E("XYZ-1", "08:00", "12:00"), E("XYZ-1", "12:30", "16:30")], [Doing]));
    }

    [Theory]
    [InlineData(DayKind.Weekend)]
    [InlineData(DayKind.Holiday)]
    [InlineData(DayKind.DayOff)]
    public void Non_workdays_get_nothing(DayKind kind)
    {
        Assert.Empty(DayFiller.Propose(new DayInfo(Monday, kind, null), Window, [], [Doing]));
    }

    [Fact]
    public void Gaps_skip_lunch_and_logged_time_including_overlap_with_lunch()
    {
        var gaps = DayFiller.Gaps(Window, [E("A-1", "07:30", "08:30"), E("A-2", "11:45", "12:45"), E("A-3", "15:00", "15:30")]);

        Assert.Equal(
            new[] { ("08:30", "11:45"), ("12:45", "15:00"), ("15:30", "16:30") }.Select(g => (TimeOnly.Parse(g.Item1), TimeOnly.Parse(g.Item2))),
            gaps);
    }

    [Fact]
    public void A_custom_window_is_respected()
    {
        var window = new Workday(new(7, 0), new(15, 30), new(11, 30), new(12, 0));

        var drafts = DayFiller.Propose(Workday, window, [], [Doing]);

        Assert.Equal("ABC-1 07:00-11:30, ABC-1 12:00-15:30", Show(drafts));
    }

    [Fact]
    public void Period_fill_covers_short_workdays_up_to_today_only()
    {
        // Thu 15 Oct .. Wed 21 Oct, today Tue 20 Oct. Fri 16 is full, Mon 19 has 2 h, Sat/Sun are weekend.
        var start = new DateOnly(2026, 10, 15);
        var days = Enumerable.Range(0, 7).Select(i => start.AddDays(i)).Select(d => new DayInfo(d,
            d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? DayKind.Weekend : DayKind.Workday, null)).ToList();
        var logged = new[]
        {
            E("XYZ-1", "08:00", "12:00").On(16), E("XYZ-1", "12:30", "16:30").On(16),
            E("XYZ-2", "08:00", "10:00"),
        };

        var plan = DayFiller.ProposePeriod(days, today: new DateOnly(2026, 10, 20), Window, logged, [Doing]);

        Assert.Equal(new[] { "2026-10-15", "2026-10-19", "2026-10-20" }.Select(DateOnly.Parse), plan.Keys.Order());
        Assert.Equal("XYZ-2 10:00-12:00, XYZ-2 12:30-16:30", Show(plan[Monday]));
        Assert.Equal("ABC-1 08:00-12:00, ABC-1 12:30-16:30", Show(plan[new DateOnly(2026, 10, 20)]));
        Assert.All(plan, p => Assert.All(p.Value, d => Assert.Equal(p.Key, d.Date)));
        Assert.All(plan, p => Assert.Equal(TimeSpan.FromHours(8), Total([.. logged.Where(e => e.Date == p.Key), .. p.Value])));
    }
}

internal static class TimeEntryTestExtensions
{
    public static TimeEntry On(this TimeEntry e, int octoberDay)
    {
        e.Date = new DateOnly(2026, 10, octoberDay);
        return e;
    }
}
