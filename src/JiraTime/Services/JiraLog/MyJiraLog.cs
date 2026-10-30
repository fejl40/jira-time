using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>
/// The signed-in user's Jira request log, for the Logs page. Everything goes through
/// <see cref="CurrentUser.Owner"/>, so a user only ever reads, hears of or clears their own requests.
/// </summary>
public sealed class MyJiraLog(JiraRequestLog log, JiraRequestFeed feed, CurrentUser user, TimeProvider clock)
{
    /// <summary>Today in the server's time zone, the day new requests are logged on.</summary>
    public DateOnly Today => log.DayOf(clock.GetUtcNow());

    /// <summary>Days with a log, newest first; today is always among them.</summary>
    public IReadOnlyList<DateOnly> Days()
    {
        var days = log.Days(user.Owner);
        return days.Contains(Today) ? days : [Today, .. days];
    }

    public async Task<JiraLogDay> ReadDayAsync(DateOnly day) => new(day, await log.ReadDayAsync(user.Owner, day));

    /// <summary>The day's file as JSON lines, for downloading.</summary>
    public Task<string> ReadDayTextAsync(DateOnly day) => log.ReadDayTextAsync(user.Owner, day);

    public Task ClearAsync() => log.ClearAsync(user.Owner);

    /// <summary>The day a request is logged on.</summary>
    public DateOnly DayOf(JiraLogEntry entry) => log.DayOf(entry.Time);

    /// <summary>Calls <paramref name="listener"/> with each of the user's requests as it starts and finishes.</summary>
    public IDisposable Subscribe(Func<JiraRequestEvent, Task> listener) => feed.Subscribe(user.Owner, listener);
}
