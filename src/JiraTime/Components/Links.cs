using System.Globalization;
using JiraTime.Domain;
using Microsoft.AspNetCore.WebUtilities;

namespace JiraTime.Components;

/// <summary>Where a timesheet link goes: the period shown and, when the link names one, the selected day.</summary>
public sealed record TimesheetLink(Period Period, DateOnly? Day)
{
    /// <summary>The day to select: the link's day when it is in the period, else today when it is, else the period's first day.</summary>
    public DateOnly Selected(DateOnly today)
    {
        if (Day is { } day && Period.Contains(day)) return day;
        return Period.Contains(today) ? today : Period.Start;
    }
}

/// <summary>App-relative links to timesheet pages, built and read.</summary>
public static class Links
{
    private const string PeriodPrefix = "period/";

    public static string To(Period period) => $"{PeriodPrefix}{Format.Iso(period.Start)}";

    /// <summary>A day of a period, selected.</summary>
    public static string To(DateOnly day) => $"{To(Period.For(day))}?day={Format.Iso(day)}";

    /// <summary>
    /// The period and day an app-relative URL shows on the timesheet, or null for other pages. Without a
    /// period in the path the link shows the period of its day, or of <paramref name="today"/>.
    /// </summary>
    public static TimesheetLink? Read(string relative, DateOnly today)
    {
        var path = relative.Split('?', '#')[0].TrimEnd('/');
        var query = relative.Contains('?') ? relative[relative.IndexOf('?')..].Split('#')[0] : "";
        var day = Format.ParseIso(QueryHelpers.ParseQuery(query).TryGetValue("day", out var d) ? d.ToString() : null);

        if (path == "") return new(Period.For(day ?? today), day);
        if (!path.StartsWith(PeriodPrefix, StringComparison.OrdinalIgnoreCase)) return null;
        return new(Period.For(Format.ParseIso(path[PeriodPrefix.Length..]) ?? day ?? today), day);
    }
}

/// <summary>Builds and reads links to the month export (export?month=yyyy-MM).</summary>
public static class ExportLinks
{
    /// <param name="month">Any day of the month.</param>
    public static string To(DateOnly month) => $"export?month={month.ToString("yyyy-MM", CultureInfo.InvariantCulture)}";

    /// <summary>The first day of the month in <paramref name="month"/> (yyyy-MM); null when it isn't one.</summary>
    public static DateOnly? Read(string? month) =>
        DateOnly.TryParseExact(month?.Trim() + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
}
