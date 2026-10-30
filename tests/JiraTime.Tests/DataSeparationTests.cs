using JiraTime.Domain;
using JiraTime.Services;

namespace JiraTime.Tests;

/// <summary>Each user sees and changes only their own stored data.</summary>
public class DataSeparationTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 19);

    private readonly TimeHarness _h = new();

    public void Dispose() => _h.Dispose();

    private RetryQueue Queue(UserKey user) => _h.For(user).Queue;
    private WorkCalendarService Calendar(UserKey user) => _h.For(user).Calendar;

    private static TimeEntry Waiting(string key, double hours) =>
        new() { IssueKey = key, Date = Day, Duration = TimeSpan.FromHours(hours), SyncPending = true };

    [Fact]
    public async Task Users_only_see_their_own_waiting_changes()
    {
        await Queue(TestUsers.Carl).AddAsync(Waiting("ABC-1", 8));
        await Queue(TestUsers.Anna).AddAsync(Waiting("XYZ-1", 3));

        Assert.Equal(["ABC-1"], (await Queue(TestUsers.Carl).GetPendingAsync(Day, Day)).Select(e => e.IssueKey));
        Assert.Equal(["XYZ-1"], (await Queue(TestUsers.Anna).GetPendingAsync(Day, Day)).Select(e => e.IssueKey));
    }

    [Fact]
    public async Task Waiting_changes_are_stored_with_their_owner()
    {
        var entry = Waiting("ABC-1", 8);
        await Queue(TestUsers.Carl).AddAsync(entry);

        Assert.Equal(TestUsers.Carl.Id, (await _h.Store.Collection<TimeEntry>().GetAsync(entry.Id))!.Owner);
    }

    [Fact]
    public async Task A_user_cannot_change_or_remove_someone_elses_waiting_change()
    {
        var carls = Waiting("ABC-1", 8);
        await Queue(TestUsers.Carl).AddAsync(carls);
        var anna = Queue(TestUsers.Anna);

        var forged = carls.Copy();
        forged.Owner = null;
        forged.Duration = TimeSpan.FromHours(1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => anna.AddAsync(forged));
        await anna.RemoveAsync(carls.Id);

        Assert.Equal(TimeSpan.FromHours(8), Assert.Single(await Queue(TestUsers.Carl).GetPendingAsync(Day, Day)).Duration);
    }

    [Fact]
    public async Task Days_off_are_per_user()
    {
        await Calendar(TestUsers.Carl).MarkDayOffAsync(Day, DayOffReason.Vacation);

        Assert.Equal(DayKind.DayOff, (await Calendar(TestUsers.Carl).GetDayAsync(Day)).Kind);
        Assert.Equal(DayKind.Workday, (await Calendar(TestUsers.Anna).GetDayAsync(Day)).Kind);

        await Calendar(TestUsers.Anna).UnmarkDayOffAsync(Day);
        Assert.Equal(DayKind.DayOff, (await Calendar(TestUsers.Carl).GetDayAsync(Day)).Kind);
    }

    [Fact]
    public async Task Nothing_works_without_a_verified_user()
    {
        var queue = new RetryQueue(_h.Store, new CurrentUser());

        await Assert.ThrowsAsync<InvalidOperationException>(() => queue.GetPendingAsync(Day, Day));
    }
}
