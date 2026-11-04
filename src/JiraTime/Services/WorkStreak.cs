using JiraTime.Domain;
using JiraTime.Services.SignIn;

namespace JiraTime.Services;

/// <summary>
/// The signed-in user's streak in this tab, for the header's flame. It is counted from the recent work each period
/// load reads anyway (<see cref="PeriodLoader"/>), so it costs no Jira request of its own; until a period has loaded,
/// or while Jira can't be read, there is none.
/// </summary>
public sealed class WorkStreak(WorkCalendarService calendar, CurrentUser user)
{
    private (UserKey? Owner, Streak? Streak) _counted;

    /// <summary>The streak last counted, while the same user is signed in.</summary>
    public Streak? Current => _counted.Owner == user.Key ? _counted.Streak : null;

    /// <summary>Raised when <see cref="Current"/> changes.</summary>
    public event Action? Changed;

    /// <summary>Counts the streak up to <paramref name="today"/> from <paramref name="recent"/>, the last <see cref="RecentWork.Days"/> days of work.</summary>
    public async Task CountAsync(DateOnly today, IReadOnlyList<TimeEntry> recent, bool hoursKnown)
    {
        Streak? streak = null;
        if (hoursKnown)
        {
            var from = today.AddDays(-RecentWork.Days);
            streak = Streak.Count(today, from, await calendar.GetDaysAsync(from, today), recent);
        }
        if (streak == Current) return;
        _counted = (user.Key, streak);
        Changed?.Invoke();
    }
}
