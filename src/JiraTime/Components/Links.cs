using JiraTime.Domain;
using Microsoft.AspNetCore.WebUtilities;

namespace JiraTime.Components;

/// <summary>Where a timesheet link goes: the period shown and, when the link names one, the selected day.</summary>
public sealed record TimesheetLink(Period Period, DateOnly? Day);

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
