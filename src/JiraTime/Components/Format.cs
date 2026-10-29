using System.Globalization;

namespace JiraTime.Components;

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

    public static string Range(DateOnly a, DateOnly b) =>
        a.Year == b.Year
            ? $"{a.ToString("d MMM", CultureInfo.InvariantCulture)} – {b.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}"
            : $"{a.ToString("d MMM yyyy", CultureInfo.InvariantCulture)} – {b.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}";

    public static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>A day written as <see cref="Iso"/>; null for anything else.</summary>
    public static DateOnly? ParseIso(string? text) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
}

public static class IssueColors
{
    // Literal class names so Tailwind picks them up.
    private static readonly string[] Bg = ["bg-blue-500", "bg-emerald-500", "bg-amber-500", "bg-violet-500", "bg-rose-500", "bg-cyan-500", "bg-lime-600", "bg-fuchsia-500"];

    /// <summary>Same issue key → same color, every time.</summary>
    public static string For(string issueKey) => Bg[issueKey.Aggregate(0, (h, c) => (h * 31 + c) & 0x7fffffff) % Bg.Length];
}
