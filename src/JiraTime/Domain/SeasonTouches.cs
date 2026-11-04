namespace JiraTime.Domain;

/// <summary>
/// The seasonal touches a day gets: snow drifting down the page in December, and a string of lights along the
/// header on a Danish public holiday (Christmas gets both). Pure logic.
/// </summary>
/// <param name="Holiday">The holiday's name when the day is one; it lights the header.</param>
public sealed record SeasonTouches(bool Snow, string? Holiday)
{
    public bool Lights => Holiday is not null;

    /// <summary>The touches of <paramref name="day"/>.</summary>
    public static SeasonTouches For(DateOnly day) => new(day.Month == 12, DanishHolidays.NameOf(day));

    /// <summary>The touches of today on the user's clock (<paramref name="zone"/>; the server's time zone when null).</summary>
    public static SeasonTouches Today(TimeProvider clock, TimeZoneInfo? zone) => For(clock.Today(zone));
}
