namespace JiraTime.Domain;

/// <summary>The background the user picked on Settings; Aurora unless they chose another.</summary>
public enum BackgroundChoice { Aurora, Sky, Plain }

/// <summary>The aurora's colours, from cool to warm as the period fills up; Neutral when there is no progress to show.</summary>
public enum AuroraPalette { Neutral, Blue, Teal, Green, Gold }

/// <summary>
/// What the page's background shows: the user's choice and, for the aurora, how far the shown period is. Pure
/// logic; Components/Layout/BackdropLayer.razor draws it.
/// </summary>
public static class Backdrop
{
    /// <summary>
    /// The aurora's palette for a period: blue when little of what the days up to today need is logged, then teal and
    /// green, gold when nothing is missing. Neutral without a period, when Jira couldn't be read or when no day needs
    /// hours yet.
    /// </summary>
    public static AuroraPalette Palette(PeriodSummary? summary)
    {
        if (summary is not { JiraHoursKnown: true }) return AuroraPalette.Neutral;
        var needed = summary.NeededSoFar;
        if (needed <= TimeSpan.Zero) return AuroraPalette.Neutral;
        if (summary.Missing <= TimeSpan.Zero) return AuroraPalette.Gold;

        var progress = (needed - summary.Missing) / needed; // a day logged over its hours doesn't make up for another
        return progress switch
        {
            < 0.4 => AuroraPalette.Blue,
            < 0.75 => AuroraPalette.Teal,
            _ => AuroraPalette.Green,
        };
    }

    /// <summary>The name a data attribute and the styles use for a value, such as "gold" or "sky".</summary>
    public static string Name<T>(T value) where T : struct, Enum => value.ToString().ToLowerInvariant();
}
