using System.Globalization;

namespace JiraTime.Components;

/// <summary>Numbers, hours and days written the way the app shows them.</summary>
public static class Format
{
    /// <summary>Decimal hours like myTE: 1.5, 0.25, 8.</summary>
    public static string Hours(TimeSpan t) =>
        t == TimeSpan.Zero ? "" : t.TotalHours.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>Decimal hours for scripts, e.g. "7.5": never empty, always with a dot.</summary>
    public static string HoursValue(TimeSpan t) => t.TotalHours.ToString("0.####", CultureInfo.InvariantCulture);

    /// <summary>Hours with a unit, "0 h" for none: 7.5 h.</summary>
    public static string HoursText(TimeSpan t) => $"{(Hours(t) is { Length: > 0 } h ? h : "0")} h";

    /// <summary>A number and a word, plural when it isn't one: "1 row", "3 rows", "2 entries".</summary>
    public static string Count(int n, string word)
    {
        if (n == 1) return $"1 {word}";
        // "entry" becomes "entries", but "day" stays "days".
        var consonantY = word.Length > 1 && word[^1] == 'y' && !"aeiou".Contains(word[^2]);
        return consonantY ? $"{n} {word[..^1]}ies" : $"{n} {word}s";
    }

    /// <summary>"1 day", "3 days".</summary>
    public static string Days(int n) => Count(n, "day");

    /// <summary>"3 Nov – 16 Nov 2026"; both years when they differ.</summary>
    public static string Range(DateOnly a, DateOnly b) =>
        a.Year == b.Year
            ? $"{Text(a, "d MMM")} – {Text(b, "d MMM yyyy")}"
            : $"{Text(a, "d MMM yyyy")} – {Text(b, "d MMM yyyy")}";

    private static string Text(DateOnly d, string format) => d.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>"Monday 3 Nov".</summary>
    public static string Day(DateOnly d) => Text(d, "dddd d MMM");

    /// <summary>"Mon 3 Nov".</summary>
    public static string ShortDay(DateOnly d) => Text(d, "ddd d MMM");

    /// <summary>"Monday 3 November".</summary>
    public static string LongDay(DateOnly d) => Text(d, "dddd d MMMM");

    /// <summary>"Mon 3": weekday and day of the month, for a list of days in one month.</summary>
    public static string WeekdayAndNumber(DateOnly d) => Text(d, "ddd d");

    /// <summary>"Mo": the weekday in two letters, for narrow columns.</summary>
    public static string TwoLetterWeekday(DateOnly d) => Text(d, "ddd")[..2];

    /// <summary>"November 2026".</summary>
    public static string Month(DateOnly d) => Text(d, "MMMM yyyy");

    /// <summary>A share from 0 to 1 as a CSS percentage: 0.625 → "62.5%".</summary>
    public static string Percent(double share) => (share * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";

    /// <summary>How long a request took: "85 ms", "1.4 s".</summary>
    public static string Duration(long milliseconds) =>
        milliseconds < 1000 ? $"{milliseconds} ms" : (milliseconds / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + " s";

    /// <summary>A size in bytes: "512 B", "1.5 KB", "2.3 MB".</summary>
    public static string Bytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => (bytes / 1024.0).ToString("0.#", CultureInfo.InvariantCulture) + " KB",
        _ => (bytes / (1024.0 * 1024)).ToString("0.#", CultureInfo.InvariantCulture) + " MB",
    };

    /// <summary>"2026-11-03".</summary>
    public static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>A day written as <see cref="Iso"/>; null for anything else.</summary>
    public static DateOnly? ParseIso(string? text) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
}
