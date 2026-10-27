namespace JiraTime.Services;

public static class TimeProviderExtensions
{
    /// <summary>Today in the server's time zone, read each time so it is right after midnight.</summary>
    public static DateOnly Today(this TimeProvider clock) => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
}
