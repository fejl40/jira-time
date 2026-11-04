using JiraTime.Domain;

namespace JiraTime.Tests.Domain;

public class PeriodFinishTests
{
    [Theory]
    [InlineData(13)]
    [InlineData(15)]
    [InlineData(16)]
    public void The_sweep_takes_about_the_same_time_for_every_period(int days)
    {
        var finish = PeriodFinish.For(days);

        Assert.InRange(finish.Step * (days - 1), 450, 650);
    }

    [Fact]
    public void The_confetti_waits_until_the_light_reaches_the_last_day()
    {
        var finish = PeriodFinish.For(16);

        Assert.True(finish.ConfettiAt > finish.Step * 15);
        Assert.True(finish.ConfettiAt < finish.Step * 15 + finish.Band);
    }

    [Theory]
    [InlineData(2, 60)]
    [InlineData(0, 60)]
    [InlineData(40, 25)]
    public void Steps_stay_between_a_crawl_and_a_blur(int columns, int step) => Assert.Equal(step, PeriodFinish.For(columns).Step);

    [Fact]
    public void The_background_glows_for_about_a_second() => Assert.InRange(PeriodFinish.For(15).Glow, 800, 1400);
}
