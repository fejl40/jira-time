namespace JiraTime.Tests;

/// <summary>A clock tests can set and move.</summary>
public sealed class TestClock(DateTimeOffset now) : TimeProvider
{
    /// <summary>A clock at noon UTC on Tuesday 27 October 2026.</summary>
    public TestClock() : this(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero)) { }

    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    public void Advance(TimeSpan by) => Now += by;
}
