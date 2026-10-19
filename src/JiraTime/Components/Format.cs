using System.Globalization;

namespace JiraTime.Components;

public static class Format
{
    /// <summary>Decimal hours like myTE: 1.5, 0.25, 8.</summary>
    public static string Hours(TimeSpan t) =>
        t == TimeSpan.Zero ? "" : t.TotalHours.ToString("0.##", CultureInfo.InvariantCulture);

    public static string Time(TimeOnly t) => t.ToString("HH:mm", CultureInfo.InvariantCulture);

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
