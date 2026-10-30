namespace JiraTime.Components.Sheet;

/// <summary>The colour of an issue's dot in the grid, the day's rows and the issue picker.</summary>
public static class IssueColors
{
    // Literal class names so Tailwind picks them up.
    private static readonly string[] Bg = ["bg-blue-500", "bg-emerald-500", "bg-amber-500", "bg-violet-500", "bg-rose-500", "bg-cyan-500", "bg-lime-600", "bg-fuchsia-500"];

    /// <summary>Same issue key → same color, every time.</summary>
    public static string For(string issueKey) => Bg[issueKey.Aggregate(0, (h, c) => (h * 31 + c) & 0x7fffffff) % Bg.Length];
}
