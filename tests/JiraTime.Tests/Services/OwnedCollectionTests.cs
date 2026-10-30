using JiraTime.Domain;
using JiraTime.Services;
using static JiraTime.Tests.Support.TestEntries;
using JiraTime.Services.SignIn;
using JiraTime.Services.Worklogs;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Services;

/// <summary>
/// An owned collection only ever reads and writes the signed-in user's documents, and the services that keep data per
/// user (the retry queue, days off) go through it.
/// </summary>
public sealed class OwnedCollectionTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 19);

    private readonly TimeHarness _h = new();

    public void Dispose() => _h.Dispose();

    private OwnedCollection<DayOff> DaysOff(UserKey user) => new(_h.Store, TestUsers.SignedIn(user));
    private RetryQueue Queue(UserKey user) => _h.For(user).Queue;
    private WorkCalendarService Calendar(UserKey user) => _h.For(user).Calendar;

    private Task StoreAsync(string id, UserKey owner, DateOnly date) =>
        _h.Store.Collection<DayOff>().UpsertAsync(new DayOff { Id = id, Owner = owner.Id, Date = date });

    [Fact]
    public async Task Getting_someone_elses_document_by_id_gives_nothing()
    {
        await StoreAsync("shared-id", TestUsers.Carl, Day);

        Assert.NotNull(await DaysOff(TestUsers.Carl).GetAsync("shared-id"));
        Assert.Null(await DaysOff(TestUsers.Anna).GetAsync("shared-id"));
        Assert.Null(await DaysOff(TestUsers.Carl).GetAsync("missing"));
    }

    [Fact]
    public async Task A_query_never_returns_someone_elses_documents()
    {
        await StoreAsync("carls", TestUsers.Carl, Day);
        await StoreAsync("annas", TestUsers.Anna, Day);
        await StoreAsync("old", TestUsers.Carl, Day.AddDays(-30));

        Assert.Equal(["carls"], (await DaysOff(TestUsers.Carl).QueryAsync(d => d.Date >= Day)).Select(d => d.Id));
        Assert.Equal(["annas"], (await DaysOff(TestUsers.Anna).QueryAsync(_ => true)).Select(d => d.Id));
    }

    [Fact]
    public async Task Saving_stamps_the_signed_in_user_as_owner()
    {
        await DaysOff(TestUsers.Carl).UpsertAsync(new DayOff { Id = "new", Owner = TestUsers.Anna.Id, Date = Day });

        Assert.Equal(TestUsers.Carl.Id, (await _h.Store.Collection<DayOff>().GetAsync("new"))!.Owner);
    }

    [Fact]
    public async Task Saving_over_someone_elses_id_throws_and_changes_nothing()
    {
        await StoreAsync("shared-id", TestUsers.Carl, Day);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DaysOff(TestUsers.Anna).UpsertAsync(new DayOff { Id = "shared-id", Date = Day.AddDays(1) }));

        var stored = await _h.Store.Collection<DayOff>().GetAsync("shared-id");
        Assert.Equal((TestUsers.Carl.Id, Day), (stored!.Owner, stored.Date));
    }

    [Fact]
    public async Task Deleting_someone_elses_document_leaves_it()
    {
        await StoreAsync("shared-id", TestUsers.Carl, Day);

        Assert.False(await DaysOff(TestUsers.Anna).DeleteAsync("shared-id"));
        Assert.True(await DaysOff(TestUsers.Carl).DeleteAsync("shared-id"));
        Assert.Null(await _h.Store.Collection<DayOff>().GetAsync("shared-id"));
    }

    [Fact]
    public async Task Nothing_works_without_a_signed_in_user()
    {
        var days = new OwnedCollection<DayOff>(_h.Store, new CurrentUser());

        await Assert.ThrowsAsync<InvalidOperationException>(() => days.QueryAsync(_ => true));
        await Assert.ThrowsAsync<InvalidOperationException>(() => days.GetAsync("any"));
    }

    [Fact]
    public async Task Users_only_see_their_own_waiting_changes()
    {
        await Queue(TestUsers.Carl).AddAsync(Waiting("ABC-1", 8, Day));
        await Queue(TestUsers.Anna).AddAsync(Waiting("XYZ-1", 3, Day));

        Assert.Equal(["ABC-1"], (await Queue(TestUsers.Carl).GetPendingAsync(Day, Day)).Select(e => e.IssueKey));
        Assert.Equal(["XYZ-1"], (await Queue(TestUsers.Anna).GetPendingAsync(Day, Day)).Select(e => e.IssueKey));
    }

    [Fact]
    public async Task Waiting_changes_are_stored_with_their_owner()
    {
        var entry = Waiting("ABC-1", 8, Day);
        await Queue(TestUsers.Carl).AddAsync(entry);

        Assert.Equal(TestUsers.Carl.Id, (await _h.Store.Collection<TimeEntry>().GetAsync(entry.Id))!.Owner);
    }

    [Fact]
    public async Task A_user_cannot_change_or_remove_someone_elses_waiting_change()
    {
        var carls = Waiting("ABC-1", 8, Day);
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
    public async Task The_retry_queue_does_nothing_without_a_verified_user()
    {
        var queue = new RetryQueue(_h.Store, new CurrentUser());

        await Assert.ThrowsAsync<InvalidOperationException>(() => queue.GetPendingAsync(Day, Day));
    }
}
