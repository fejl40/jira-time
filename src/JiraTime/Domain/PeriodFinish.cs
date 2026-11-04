namespace JiraTime.Domain;

/// <summary>
/// The timing of the finish when a period becomes complete (wwwroot/finish.js), in milliseconds: a light sweeps
/// across the day columns one after another while the background glows, and the confetti bursts as the sweep reaches
/// the last day. Pure logic, so the sweep takes about the same time whether the period has 13 days or 16.
/// </summary>
public sealed record PeriodFinish(int Step, int Band, int Glow, int ConfettiAt)
{
    /// <summary>About how long the light takes to cross all the columns.</summary>
    public const int Sweep = 600;

    /// <summary>The finish for a grid of <paramref name="columns"/> days.</summary>
    public static PeriodFinish For(int columns)
    {
        var count = Math.Max(1, columns);
        var step = Math.Clamp(Sweep / count, 25, 60); // few columns don't crawl, many don't blur together
        const int band = 360; // how long each column stays lit
        var lastLights = step * (count - 1);
        return new PeriodFinish(step, band, Glow: 1100, ConfettiAt: lastLights + band / 2);
    }
}
