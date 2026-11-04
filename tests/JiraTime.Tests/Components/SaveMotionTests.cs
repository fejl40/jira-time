using JiraTime.Components.Sheet;
using JiraTime.Domain;

namespace JiraTime.Tests.Components;

public class SaveMotionTests
{
    /// <summary>16–31 October 2026: Friday 16 to Sunday 18, then Monday 19 to Sunday 25, then Monday 26 to Saturday 31.</summary>
    private static readonly Period October = Period.For(new DateOnly(2026, 10, 20));

    private static DateOnly Oct(int day) => new(2026, 10, day);

    private static SaveChoreography StepsFor(params int[] days) =>
        SaveChoreography.For(October, days.Select(Oct).ToHashSet(), flyTicks: true);

    [Fact]
    public void Only_saved_days_fly_in_day_order()
    {
        Assert.Equal(["2026-10-20", "2026-10-27"], StepsFor(27, 20).Fly);
    }

    [Fact]
    public void Each_saved_week_ripples_from_its_first_day_one_cell_after_another()
    {
        var ripple = StepsFor(21).Ripple;

        Assert.Equal(Enumerable.Range(19, 7).Select(d => $"2026-10-{d}"), ripple.Select(c => c.Day));
        Assert.Equal(Enumerable.Range(0, 7).Select(i => SaveChoreography.RippleStart + i * 40), ripple.Select(c => c.At));
    }

    [Fact]
    public void Weeks_cut_by_the_period_ripple_only_their_days_in_it()
    {
        var ripple = StepsFor(16, 30).Ripple;

        Assert.Equal(["2026-10-16", "2026-10-17", "2026-10-18", "2026-10-26", "2026-10-27", "2026-10-28", "2026-10-29", "2026-10-30", "2026-10-31"],
            ripple.Select(c => c.Day));
        Assert.Equal(SaveChoreography.RippleStart, ripple.Single(c => c.Day == "2026-10-26").At); // weeks ripple side by side
        Assert.Equal(SaveChoreography.RippleStart, ripple[0].At);
    }

    [Fact]
    public void The_whole_choreography_ends_in_under_a_second()
    {
        var steps = StepsFor(19, 25);
        const int tick = 300; // --motion in Styles/motion.css

        Assert.True(steps.Ripple.Max(c => c.At) + SaveChoreography.Pulse <= SaveChoreography.EndsBy);
        Assert.True(steps.TicksAt + tick <= SaveChoreography.EndsBy);
        Assert.True(SaveChoreography.Flight <= SaveChoreography.EndsBy);
        Assert.True(SaveChoreography.EndsBy <= 900);
    }

    [Fact]
    public void Nothing_saved_flies_nothing_and_sends_no_ticks()
    {
        var steps = StepsFor();

        Assert.Empty(steps.Fly);
        Assert.Empty(steps.Ripple);
        Assert.False(steps.FlyTicks);
    }

    [Fact]
    public void The_steps_come_once_after_each_save_also_when_nothing_saved()
    {
        var motion = new SaveMotion();
        Assert.Null(motion.TakeChoreography(October));

        motion.Start();
        motion.Saved(Oct(20), sentToJira: false);
        motion.Finish();
        var steps = motion.TakeChoreography(October);

        Assert.Equal(["2026-10-20"], steps?.Fly);
        Assert.False(steps?.FlyTicks); // the day waits in the retry queue, so no tick goes to Jira's status
        Assert.Null(motion.TakeChoreography(October));

        motion.Start();
        motion.Finish();
        Assert.Empty(motion.TakeChoreography(October)!.Fly);
    }
}
