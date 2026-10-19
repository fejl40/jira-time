using JiraTime.Storage;

namespace JiraTime.Domain;

public enum DayOffReason { Vacation, Sick, Other }

/// <summary>A workday the user marked as not worked (id = the date, yyyy-MM-dd).</summary>
[DocumentCollection("days-off")]
public class DayOff : IDocument
{
    public string Id { get; set; } = "";
    public DateOnly Date { get; set; }
    public DayOffReason Reason { get; set; }

    public static string IdFor(DateOnly date) => date.ToString("yyyy-MM-dd");
}

/// <summary>When a normal workday runs. The lunch break is not logged.</summary>
public record Workday(TimeOnly Start, TimeOnly End, TimeOnly LunchStart, TimeOnly LunchEnd)
{
    public static readonly Workday Default = new(new(8, 0), new(16, 30), new(12, 0), new(12, 30));

    /// <summary>Hours each workday should have.</summary>
    public static readonly TimeSpan Target = TimeSpan.FromHours(8);

    /// <returns>An error message, or null when the times make sense.</returns>
    public string? Validate()
    {
        if (End <= Start) return "The workday must end after it starts.";
        if (LunchEnd < LunchStart) return "Lunch must end after it starts.";
        if (LunchStart < Start || LunchEnd > End) return "Lunch must be inside the workday.";
        return null;
    }
}

public enum DayKind { Workday, Weekend, Holiday, DayOff }

/// <summary>What kind of day a date is and how many hours it should have.</summary>
public record DayInfo(DateOnly Date, DayKind Kind, string? Name)
{
    public bool IsWorkday => Kind == DayKind.Workday;
    public TimeSpan Expected => IsWorkday ? Workday.Target : TimeSpan.Zero;
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

    /// <summary>The kind of day, given the days the user marked off.</summary>
    public static DayInfo Classify(DateOnly date, IReadOnlyDictionary<DateOnly, DayOff> daysOff)
    {
        if (NameOf(date) is { } holiday) return new DayInfo(date, DayKind.Holiday, holiday);
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return new DayInfo(date, DayKind.Weekend, null);
        if (daysOff.TryGetValue(date, out var off)) return new DayInfo(date, DayKind.DayOff, off.Reason.ToString());
        return new DayInfo(date, DayKind.Workday, null);
    }
}
