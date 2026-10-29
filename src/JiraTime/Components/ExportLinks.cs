using System.Globalization;

namespace JiraTime.Components;

/// <summary>Builds and reads links to the month export (export?month=yyyy-MM).</summary>
public static class ExportLinks
{
    /// <param name="month">Any day of the month.</param>
    public static string To(DateOnly month) => $"export?month={month.ToString("yyyy-MM", CultureInfo.InvariantCulture)}";

    /// <summary>The first day of the month in <paramref name="month"/> (yyyy-MM); null when it isn't one.</summary>
    public static DateOnly? Read(string? month) =>
        DateOnly.TryParseExact(month?.Trim() + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
}
