using JiraTime.Domain;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Domain;

public class SkyTests
{
    private static readonly TimeZoneInfo Copenhagen =
        TimeZoneInfo.CreateCustomTimeZone("UTC+1", TimeSpan.FromHours(1), "UTC+1", "UTC+1");

    private static SkyState At(int hour, int minute = 0) => Sky.At(new TimeOnly(hour, minute));

    [Theory]
    [InlineData(2, 0, SkyPhase.Night)]
    [InlineData(5, 0, SkyPhase.Dawn)]
    [InlineData(8, 0, SkyPhase.Day)]
    [InlineData(12, 0, SkyPhase.Day)]
    [InlineData(17, 0, SkyPhase.Dusk)]
    [InlineData(20, 0, SkyPhase.Night)]
    [InlineData(23, 59, SkyPhase.Night)]
    public void Each_phase_starts_at_its_time(int hour, int minute, SkyPhase phase)
    {
        var sky = At(hour, minute);

        Assert.Equal(phase, sky.Phase);
        Assert.Equal(1, sky.Opacity(phase));
    }

    [Fact]
    public void The_last_hour_of_a_phase_blends_into_the_next()
    {
        Assert.Equal(new SkyState(SkyPhase.Day, SkyPhase.Dusk, 0), At(15, 59));
        Assert.Equal(new SkyState(SkyPhase.Day, SkyPhase.Dusk, 0.5), At(16, 30));
        Assert.Equal(new SkyState(SkyPhase.Dawn, SkyPhase.Day, 0.75), At(7, 45));
    }

    [Fact]
    public void Night_blends_into_dawn_in_its_last_hour()
    {
        var sky = At(4, 30);

        Assert.Equal((SkyPhase.Night, SkyPhase.Dawn), (sky.Phase, sky.Next));
        Assert.Equal((0.5, 0.5, 0.0), (sky.Opacity(SkyPhase.Night), sky.Opacity(SkyPhase.Dawn), sky.Opacity(SkyPhase.Day)));
    }

    [Fact]
    public void The_sky_is_on_the_users_clock()
    {
        var clock = new TestClock(DateTimeOffset.Parse("2026-11-04T19:30:00Z")); // 20:30 in Copenhagen

        Assert.Equal(SkyPhase.Dusk, Sky.Now(clock).Phase); // the server's zone (UTC): the last of dusk
        Assert.Equal(SkyPhase.Night, Sky.Now(clock, Copenhagen).Phase);
    }

    [Fact]
    public void The_sky_moves_on_as_the_clock_does()
    {
        var clock = new TestClock(DateTimeOffset.Parse("2026-11-04T15:50:00Z"));
        var before = Sky.Now(clock, Copenhagen); // 16:50

        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.True(Sky.Now(clock, Copenhagen).Blend > before.Blend);
        Assert.Equal(SkyPhase.Day, before.Phase);
    }
}
