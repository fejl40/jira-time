using JiraTime.Domain;

namespace JiraTime.Components.Sheet;

/// <summary>
/// The last save, for its animation: days saved, and those Jira got completely. After a save it gives the steps
/// of the choreography once (<see cref="TakeChoreography"/>), which wwwroot/flight.js plays.
/// </summary>
public sealed class SaveMotion
{
    private bool _finished;

    public HashSet<DateOnly> JustSaved { get; } = [];
    public HashSet<DateOnly> SentToJira { get; } = [];

    /// <summary>A save begins: the last one's days no longer animate.</summary>
    public void Start()
    {
        JustSaved.Clear();
        SentToJira.Clear();
        _finished = false;
    }

    public void Saved(DateOnly day, bool sentToJira)
    {
        JustSaved.Add(day);
        if (sentToJira) SentToJira.Add(day);
    }

    /// <summary>Called after the saved work has reloaded, so the animation starts from the saved cells.</summary>
    public void Finish() => _finished = true;

    /// <summary>Once after a save, also one where no day could be saved: the steps to play on <paramref name="period"/>.</summary>
    public SaveChoreography? TakeChoreography(Period period)
    {
        if (!_finished) return null;
        _finished = false;
        return SaveChoreography.For(period, JustSaved, SentToJira.Count > 0);
    }
}

/// <summary>A cell of a saved week that pulses, <paramref name="At"/> milliseconds after the choreography starts.</summary>
public sealed record RippleCell(string Day, int At);

/// <summary>
/// What plays after Save all, in milliseconds from its start: each saved day's line flies from where the modal
/// showed it into its day, the cells of each saved week pulse one after another, then the ticks fly from the
/// saved cells to the period status. All of it ends by <see cref="EndsBy"/>.
/// </summary>
/// <param name="Fly">Days (yyyy-MM-dd) whose line flies into its day; the lines of other days fade where they are.</param>
/// <param name="FlyTicks">Some day went to Jira completely, so the ticks fly.</param>
public sealed record SaveChoreography(IReadOnlyList<string> Fly, IReadOnlyList<RippleCell> Ripple, bool FlyTicks)
{
    /// <summary>How long a line takes to fly into its day.</summary>
    public const int Flight = 380;
    /// <summary>The ripple starts as the lines land, before they are quite there.</summary>
    public const int RippleStart = 300;
    /// <summary>Delay between one cell's pulse and the next in a week.</summary>
    public const int RippleStagger = 40;
    /// <summary>How long one cell pulses.</summary>
    public const int Pulse = 240;
    /// <summary>The whole choreography is over by then; the ticks share out what is left.</summary>
    public const int EndsBy = 900;

    /// <summary>When the ticks leave the saved cells: as the last cell of the ripple starts to pulse.</summary>
    public int TicksAt => Ripple.Count == 0 ? RippleStart : Ripple.Max(cell => cell.At);

    /// <summary>The steps for the days saved in <paramref name="period"/>, in day order.</summary>
    public static SaveChoreography For(Period period, IReadOnlySet<DateOnly> saved, bool flyTicks)
    {
        var days = period.Days.Where(saved.Contains).ToList();
        var fly = days.Select(Format.Iso).ToList();
        return new SaveChoreography(fly, RippleOf(period, days), flyTicks && days.Count > 0);
    }

    /// <summary>Every day of each saved week (Monday to Sunday, within the period), each week from its first day.</summary>
    private static List<RippleCell> RippleOf(Period period, List<DateOnly> saved)
    {
        var weeks = saved.Select(WeekStart).Distinct().ToHashSet();
        var ripple = new List<RippleCell>();
        foreach (var day in period.Days)
        {
            if (!weeks.Contains(WeekStart(day))) continue;
            var place = period.Days.Count(d => d < day && WeekStart(d) == WeekStart(day));
            ripple.Add(new RippleCell(Format.Iso(day), RippleStart + place * RippleStagger));
        }
        return ripple;
    }

    private static DateOnly WeekStart(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
}
