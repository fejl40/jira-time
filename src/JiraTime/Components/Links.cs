namespace JiraTime.Components;

/// <summary>App-relative links to timesheet pages.</summary>
public static class Links
{
    public static string To(Domain.Period period) => $"period/{Format.Iso(period.Start)}";

    /// <summary>A day of a period, selected.</summary>
    public static string To(DateOnly day) => $"{To(Domain.Period.For(day))}?day={Format.Iso(day)}";
}
