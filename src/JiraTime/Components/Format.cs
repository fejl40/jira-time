using System.Globalization;

namespace JiraTime.Components;

public static class Format
{
    /// <summary>Decimal hours like myTE: 1.5, 0.25, 8.</summary>
    public static string Hours(TimeSpan t) =>
        t == TimeSpan.Zero ? "" : t.TotalHours.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>
    /// Reads hours typed by the user: decimals with a dot or comma (1.5, 1,5) or hours and minutes (1:30).
    /// Rounded to whole minutes. Null when it can't be read or is not positive.
    /// </summary>
    public static TimeSpan? ParseHours(string? text)
    {
        var s = text?.Trim().TrimEnd('h', 'H').Trim() ?? "";
        TimeSpan value;
        if (s.Split(':') is [var h, var m])
        {
            if (!int.TryParse(h, NumberStyles.None, CultureInfo.InvariantCulture, out var hours)
                || !int.TryParse(m, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) || minutes >= 60) return null;
            value = new TimeSpan(hours, minutes, 0);
        }
        else if (decimal.TryParse(s.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d))
            value = TimeSpan.FromMinutes((double)Math.Round(d * 60));
        else return null;
        return value > TimeSpan.Zero ? value : null;
    }

    public static string Range(DateOnly a, DateOnly b) =>
        a.Year == b.Year
            ? $"{a.ToString("d MMM", CultureInfo.InvariantCulture)} – {b.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}"
            : $"{a.ToString("d MMM yyyy", CultureInfo.InvariantCulture)} – {b.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}";

    public static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

public static class IssueColors
{
    // Literal class names so Tailwind picks them up.
    private static readonly string[] Bg = ["bg-blue-500", "bg-emerald-500", "bg-amber-500", "bg-violet-500", "bg-rose-500", "bg-cyan-500", "bg-lime-600", "bg-fuchsia-500"];

    /// <summary>Same issue key → same color, every time.</summary>
    public static string For(string issueKey) => Bg[issueKey.Aggregate(0, (h, c) => (h * 31 + c) & 0x7fffffff) % Bg.Length];
}
