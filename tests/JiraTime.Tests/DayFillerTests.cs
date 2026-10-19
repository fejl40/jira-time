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
        Assert.Equal("XYZ-2 6h", Show(plan[Monday]));
        Assert.Equal("ABC-1 8h", Show(plan[new DateOnly(2026, 10, 20)]));
        Assert.All(plan, p => Assert.All(p.Value, d => Assert.Equal(p.Key, d.Date)));
    }
}
