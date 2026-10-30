using JiraTime.Domain;

namespace JiraTime.Components;

/// <summary>
/// How time off looks in the grid, the day list and the day header: an icon and a tint per reason
/// (Styles/app.css, .day-off-*), stripes when the whole day is off, and a short label.
/// </summary>
public static class DayOffLook
{
    /// <summary>A sun for vacation, a face with a thermometer for sick, a calendar for other time off.</summary>
    public static string Icon(DayOffReason reason) => reason switch
    {
        DayOffReason.Vacation => "☀️",
        DayOffReason.Sick => "🤒",
        _ => "🗓️",
    };

    /// <summary>The reason as the user reads it: "Vacation", "Sick", "Time off".</summary>
    public static string Name(DayOffReason reason) => reason == DayOffReason.Other ? "Time off" : reason.ToString();

    /// <summary>"Vacation · whole day" or "Sick · 4 h off"; null on a day with no time off.</summary>
    public static string? Label(DayInfo? info) => info switch
    {
        { Reason: { } reason, Kind: DayKind.DayOff } => $"{Name(reason)} · whole day",
        { Reason: { } reason, HasPartialDayOff: true } => $"{Name(reason)} · {Format.Hours(info.Off)} h off",
        _ => null,
    };

    /// <summary>"Vacation" or "Sick 4 h", for the narrow day list on phones; null on a day with no time off.</summary>
    public static string? ShortLabel(DayInfo? info) => info switch
    {
        { Reason: { } reason, Kind: DayKind.DayOff } => Name(reason),
        { Reason: { } reason, HasPartialDayOff: true } => $"{Name(reason)} {Format.Hours(info.Off)} h",
        _ => null,
    };

    /// <summary>Classes for the day's column or list entry: the reason's tint, striped when the whole day is off.</summary>
    public static string Classes(DayInfo? info) => info switch
    {
        { Reason: { } reason, Kind: DayKind.DayOff } => $"{Tint(reason)} day-off-whole",
        { Reason: { } reason, HasPartialDayOff: true } => $"{Tint(reason)} day-off-partial",
        _ => "",
    };

    /// <summary>Classes for the badge in the day header, tinted like the day's column.</summary>
    public static string Badge(DayOffReason reason) => $"{Tint(reason)} day-off-badge";

    /// <summary>The reason in lower case, for data-day-off attributes that browser checks read.</summary>
    public static string? Data(DayInfo? info) => info?.Reason?.ToString().ToLowerInvariant();

    private static string Tint(DayOffReason reason) => reason switch
    {
        DayOffReason.Vacation => "day-off-vacation",
        DayOffReason.Sick => "day-off-sick",
        _ => "day-off-other",
    };
}
