namespace JiraTime.Domain;

/// <summary>Adding up spans of time.</summary>
public static class TimeSpanSum
{
    public static TimeSpan Sum(this IEnumerable<TimeSpan> spans) => spans.Aggregate(TimeSpan.Zero, (total, span) => total + span);
}
