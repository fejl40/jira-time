using JiraTime.Domain;
using JiraTime.Tests.Support;
using static JiraTime.Tests.Support.TestEntries;

namespace JiraTime.Tests.Domain;

public class BackdropTests
{
    // 16-31 Oct 2026; up to Wednesday 21 the workdays Fri 16, Mon 19, Tue 20 and Wed 21 need 32 hours.
    private static readonly Period Second = Period.For(new DateOnly(2026, 10, 16));
    private static readonly DateOnly Wednesday = new(2026, 10, 21);

    private static DateOnly October(int day) => new(2026, 10, day);

    private static PeriodSummary Logged(params (int Day, double Hours)[] days) =>
        PeriodSummary.For(Second, TestSheets.Days(Second), days.Select(d => Saved("ABC-1", d.Hours, October(d.Day))).ToList(), Wednesday);

    [Fact]
    public void Nothing_logged_is_blue() => Assert.Equal(AuroraPalette.Blue, Backdrop.Palette(Logged()));

    [Fact]
    public void Half_logged_is_teal() => Assert.Equal(AuroraPalette.Teal, Backdrop.Palette(Logged((16, 8), (19, 8))));

    [Fact]
    public void Most_logged_is_green() => Assert.Equal(AuroraPalette.Green, Backdrop.Palette(Logged((16, 8), (19, 8), (20, 8), (21, 4))));

    [Fact]
    public void Nothing_missing_up_to_today_is_gold() =>
        Assert.Equal(AuroraPalette.Gold, Backdrop.Palette(Logged((16, 8), (19, 8), (20, 8), (21, 8))));

    [Fact]
    public void Extra_hours_on_one_day_do_not_make_up_for_another() =>
        Assert.Equal(AuroraPalette.Blue, Backdrop.Palette(Logged((16, 12))));

    [Fact]
    public void Without_a_period_the_palette_is_neutral() => Assert.Equal(AuroraPalette.Neutral, Backdrop.Palette(null));

    [Fact]
    public void When_Jira_could_not_be_read_the_palette_is_neutral()
    {
        var unknown = PeriodSummary.For(Second, TestSheets.Days(Second), [], Wednesday, hoursKnown: false);

        Assert.Equal(AuroraPalette.Neutral, Backdrop.Palette(unknown));
    }

    [Fact]
    public void A_period_that_needs_no_hours_yet_is_neutral()
    {
        var future = PeriodSummary.For(Second, TestSheets.Days(Second), [], new DateOnly(2026, 10, 10));

        Assert.Equal(AuroraPalette.Neutral, Backdrop.Palette(future));
    }

    [Fact]
    public void Names_are_lower_case() => Assert.Equal("gold", Backdrop.Name(AuroraPalette.Gold));
}
