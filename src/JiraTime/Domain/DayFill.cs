namespace JiraTime.Domain;

/// <summary>How a day's water stands: still rising, at the brim, or with more saved than the day needs.</summary>
public enum DayFillState { Filling, Full, Over }

/// <summary>
/// How full a day is, for the water that fills its column in the grid: saved hours against the hours the day
/// needs, from 0 to 1. Days that need no hours (weekends, holidays, whole days off) have no water, nor does any
/// day while Jira can't be read, as its hours are unknown then.
/// </summary>
public readonly record struct DayFill(double Level, DayFillState State)
{
    /// <summary>The fill of <paramref name="date"/> in <paramref name="sheet"/>; null when the day has no water.</summary>
    public static DayFill? Of(PeriodSheet sheet, DateOnly date)
    {
        if (!sheet.HoursKnown) return null;
        var day = sheet.Day(date);
        return Of(day.Logged, day.Expected);
    }

    /// <summary>The fill for <paramref name="logged"/> hours on a day that needs <paramref name="expected"/>.</summary>
    public static DayFill? Of(TimeSpan logged, TimeSpan expected)
    {
        if (expected <= TimeSpan.Zero) return null;
        if (logged > expected) return new DayFill(1, DayFillState.Over);
        if (logged == expected) return new DayFill(1, DayFillState.Full);
        var level = Math.Max(0, logged / expected);
        return new DayFill(level, DayFillState.Filling);
    }

    /// <summary>The state as a browser check reads it: filling, full or over.</summary>
    public string Data => State.ToString().ToLowerInvariant();
}

/// <summary>
/// The water of every day of the period on screen, and how often each day's level changed while the user watches,
/// so a wave runs once on the surface per change. The levels at the first look of a period are no change, so
/// opening a period stays still.
/// </summary>
public sealed class DayFills
{
    private PeriodSheet? _sheet;
    private Dictionary<DateOnly, DayFill?> _fills = [];
    private readonly Dictionary<DateOnly, int> _waves = [];

    /// <summary>Takes the days of <paramref name="sheet"/> as they stand now; another period starts afresh.</summary>
    public void Show(PeriodSheet sheet)
    {
        var fills = sheet.Period.Days.ToDictionary(day => day, day => DayFill.Of(sheet, day));
        if (sheet != _sheet) _waves.Clear();
        else
        {
            foreach (var (day, fill) in fills)
            {
                if (Level(fill) != Level(Fill(day))) _waves[day] = Wave(day) + 1;
            }
        }
        (_sheet, _fills) = (sheet, fills);
    }

    /// <summary>How full the day is; null when it has no water.</summary>
    public DayFill? Fill(DateOnly day) => _fills.GetValueOrDefault(day);

    /// <summary>How many times the day's level changed; 0 before the first change, so no wave shows.</summary>
    public int Wave(DateOnly day) => _waves.GetValueOrDefault(day);

    /// <summary>A day without water counts as empty.</summary>
    private static double Level(DayFill? fill) => fill?.Level ?? 0;
}
