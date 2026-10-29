using JiraTime.Storage;

namespace JiraTime.Domain;

public enum DayOffReason { Vacation, Sick, Other }

/// <summary>A workday the user marked as not worked (id = owner + date).</summary>
[DocumentCollection("days-off")]
public class DayOff : IOwnedDocument
{
    public string Id { get; set; } = "";
    /// <summary><see cref="UserKey.Id"/> of the user. Empty on data from before multi-user support.</summary>
    public string? Owner { get; set; }
    public DateOnly Date { get; set; }
    public DayOffReason Reason { get; set; }

    /// <summary>Hours off. Null (or at least the day's hours) means the whole day.</summary>
    public TimeSpan? Hours { get; set; }

    public static string IdFor(string owner, DateOnly date) => $"{owner}-{date:yyyy-MM-dd}";
}

public enum DayKind { Workday, Weekend, Holiday, DayOff }

/// <summary>What kind of day a date is and how many hours it should have.</summary>
/// <param name="Name">Holiday name, or the reason for time off.</param>
public record DayInfo(DateOnly Date, DayKind Kind, string? Name)
{
    /// <summary>Hours a workday has unless the user set other hours.</summary>
    public static readonly TimeSpan Target = TimeSpan.FromHours(8);

    /// <summary>Hours this weekday has for the user, before time off.</summary>
    public TimeSpan Hours { get; init; } = Target;

    /// <summary>Part of a workday taken off (a partial day off). Zero otherwise.</summary>
    public TimeSpan Off { get; init; }

    public bool IsWorkday => Kind == DayKind.Workday;
    public bool HasPartialDayOff => IsWorkday && Off > TimeSpan.Zero;

    /// <summary>Hours to log: the day's hours minus time off, zero on non-workdays.</summary>
    public TimeSpan Expected => IsWorkday && Hours > Off ? Hours - Off : TimeSpan.Zero;

    /// <summary>Hours still missing when <paramref name="logged"/> is logged: expected minus logged, never negative.</summary>
    public TimeSpan MissingWith(TimeSpan logged) => Expected > logged ? Expected - logged : TimeSpan.Zero;

    /// <summary>The kind of day and its hours, given the user's days off and work hours (default: 8 h Monday to Friday).</summary>
    public static DayInfo For(DateOnly date, IReadOnlyDictionary<DateOnly, DayOff> daysOff, UserSettings? settings = null)
    {
        if (DanishHolidays.NameOf(date) is { } holiday) return new DayInfo(date, DayKind.Holiday, holiday) { Hours = TimeSpan.Zero };
        var hours = (settings ?? new UserSettings()).HoursOn(date.DayOfWeek);
        if (hours <= TimeSpan.Zero)
            return new DayInfo(date, DayKind.Weekend, date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? null : "No work hours") { Hours = TimeSpan.Zero };
        if (daysOff.TryGetValue(date, out var off))
        {
            if (off.Hours is not { } partial || partial >= hours)
                return new DayInfo(date, DayKind.DayOff, off.Reason.ToString()) { Hours = hours, Off = hours };
            return new DayInfo(date, DayKind.Workday, off.Reason.ToString()) { Hours = hours, Off = partial };
        }
        return new DayInfo(date, DayKind.Workday, null) { Hours = hours };
    }
}

/// <summary>Hours each weekday needs, per user (id = owner).</summary>
[DocumentCollection("user-settings")]
public class UserSettings : IOwnedDocument
{
    public string Id { get; set; } = "";
    public string? Owner { get; set; }

    public Dictionary<DayOfWeek, TimeSpan> WorkHours { get; set; } = StandardWeek();

    public TimeSpan HoursOn(DayOfWeek day) => WorkHours.GetValueOrDefault(day);

    /// <summary>8 hours Monday to Friday.</summary>
    public static Dictionary<DayOfWeek, TimeSpan> StandardWeek() =>
        Enum.GetValues<DayOfWeek>().ToDictionary(d => d,
            d => d is DayOfWeek.Saturday or DayOfWeek.Sunday ? TimeSpan.Zero : DayInfo.Target);

    /// <returns>Why the hours can't be used, or null.</returns>
    public static string? Validate(IReadOnlyDictionary<DayOfWeek, TimeSpan> hours)
    {
        foreach (var (day, h) in hours)
        {
            if (h < TimeSpan.Zero || h > TimeEntry.MaxDuration) return $"{day}: hours must be between 0 and 24.";
            if (h.Ticks % TimeSpan.TicksPerMinute != 0) return $"{day}: hours must be whole minutes.";
        }
        return null;
    }
}

/// <summary>Official Danish public holidays (helligdage).</summary>
public static class DanishHolidays
{
    public static IReadOnlyDictionary<DateOnly, string> For(int year)
    {
        var easter = EasterSunday(year);
        var days = new Dictionary<DateOnly, string>
        {
            [new(year, 1, 1)] = "New Year's Day",
            [easter.AddDays(-3)] = "Maundy Thursday",
            [easter.AddDays(-2)] = "Good Friday",
            [easter] = "Easter Sunday",
            [easter.AddDays(1)] = "Easter Monday",
            [easter.AddDays(39)] = "Ascension Day",
            [easter.AddDays(49)] = "Whit Sunday",
            [easter.AddDays(50)] = "Whit Monday",
            [new(year, 12, 25)] = "Christmas Day",
            [new(year, 12, 26)] = "Boxing Day",
        };
        // Great Prayer Day (Store Bededag) was abolished from 2024.
        if (year < 2024) days[easter.AddDays(26)] = "Great Prayer Day";
        return days;
    }

    public static string? NameOf(DateOnly date) => For(date.Year).TryGetValue(date, out var name) ? name : null;

    /// <summary>Gregorian Easter Sunday (anonymous Gregorian algorithm).</summary>
    public static DateOnly EasterSunday(int year)
    {
        int a = year % 19, b = year / 100, c = year % 100, d = b / 4, e = b % 4;
        int f = (b + 8) / 25, g = (b - f + 1) / 3, h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4, k = c % 4, l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31, day = (h + l - 7 * m + 114) % 31 + 1;
        return new DateOnly(year, month, day);
    }
}
