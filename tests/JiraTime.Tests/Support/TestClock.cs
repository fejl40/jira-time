namespace JiraTime.Tests.Support;

/// <summary>
/// A clock tests can set and move. Without a time it is noon UTC on Tuesday 27 October 2026, in UTC. Timers fire at
/// once and move the clock on to when they were due, so waits (such as a retry after a 429) take no real time
/// and tests can see how long they waited.
/// </summary>
public sealed class TestClock(DateTimeOffset now) : TimeProvider
{
    private readonly Lock _lock = new();
    private DateTimeOffset _now = now;

    public TestClock() : this(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero)) { }

    public DateTimeOffset Now
    {
        get { lock (_lock) return _now; }
        set { lock (_lock) _now = value; }
    }

    public override DateTimeOffset GetUtcNow() => Now;
    /// <summary>The server's time zone; UTC unless a test sets another.</summary>
    public TimeZoneInfo Zone { get; set; } = TimeZoneInfo.Utc;

    public override TimeZoneInfo LocalTimeZone => Zone;
    public void Advance(TimeSpan by) { lock (_lock) _now += by; }

    /// <summary>Moves the clock to <paramref name="time"/> unless it is already later: waits that overlap don't add up.</summary>
    private void MoveTo(DateTimeOffset time) { lock (_lock) if (time > _now) _now = time; }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
        new JumpingTimer(this, callback, state, dueTime);

    private sealed class JumpingTimer : ITimer
    {
        private readonly TestClock _clock;
        private readonly TimerCallback _callback;
        private readonly object? _state;

        public JumpingTimer(TestClock clock, TimerCallback callback, object? state, TimeSpan dueTime)
        {
            (_clock, _callback, _state) = (clock, callback, state);
            Change(dueTime, Timeout.InfiniteTimeSpan);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            if (dueTime == Timeout.InfiniteTimeSpan) return true;
            _clock.MoveTo(_clock.Now + dueTime);
            ThreadPool.QueueUserWorkItem(_ => _callback(_state));
            return true;
        }

        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
