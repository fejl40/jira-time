namespace JiraTime.Domain;

/// <summary>The parts of the day the sky background shows.</summary>
public enum SkyPhase { Night, Dawn, Day, Dusk }

/// <summary>
/// The sky at one moment: the phase it is in and how far (0 to 1) it has blended into the next one.
/// </summary>
public sealed record SkyState(SkyPhase Phase, SkyPhase Next, double Blend)
{
    /// <summary>How much of a phase's layer shows: the current phase fades out as the next one fades in.</summary>
    public double Opacity(SkyPhase phase)
    {
        if (phase == Phase) return Math.Round(1 - Blend, 2);
        return phase == Next ? Blend : 0;
    }
}

/// <summary>
/// The sky at the user's time of day. Pure logic: each phase starts at a fixed local time, and the last hour of a
/// phase blends into the next, so the sky moves on little by little while the page stays open.
/// </summary>
public static class Sky
{
    /// <summary>The phases in the order of the day, with the local time each starts.</summary>
    public static readonly IReadOnlyList<(SkyPhase Phase, TimeOnly Starts)> Phases =
    [
        (SkyPhase.Dawn, new TimeOnly(5, 0)),
        (SkyPhase.Day, new TimeOnly(8, 0)),
        (SkyPhase.Dusk, new TimeOnly(17, 0)),
        (SkyPhase.Night, new TimeOnly(20, 0)),
    ];

    /// <summary>How long before the next phase starts the sky begins to blend into it.</summary>
    public static readonly TimeSpan BlendTime = TimeSpan.FromHours(1);

    /// <summary>The sky at a local time of day.</summary>
    public static SkyState At(TimeOnly time)
    {
        var current = Phases.Count - 1; // before dawn it is still the night that started yesterday
        for (var i = 0; i < Phases.Count; i++)
        {
            if (Phases[i].Starts <= time) current = i;
        }
        var next = Phases[(current + 1) % Phases.Count];
        var untilNext = next.Starts - time; // wraps past midnight
        var blend = untilNext >= BlendTime ? 0 : Math.Round(1 - untilNext / BlendTime, 2);
        return new SkyState(Phases[current].Phase, next.Phase, blend);
    }

    /// <summary>The sky now in <paramref name="zone"/> (the server's time zone when null), like <see cref="Clock.Today"/>.</summary>
    public static SkyState Now(TimeProvider clock, TimeZoneInfo? zone = null) =>
        At(TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone ?? clock.LocalTimeZone).DateTime));
}
