using JiraTime.Domain;
using JiraTime.Services;

namespace JiraTime.Tests;

/// <summary>Entries from when the app kept logged time itself: dropped once Jira has them, after a backup.</summary>
public class RetireLocalTimeTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 19);

    private readonly TimeHarness _h = new();

    public void Dispose() => _h.Dispose();

    private async Task<TimeEntry> Stored(string key, UserKey owner, string? worklog, bool pending = false, bool delete = false)
    {
        var e = new TimeEntry
        {
            IssueKey = key, Date = Day, Duration = TimeSpan.FromHours(1), Owner = owner.Id,
            JiraWorklogId = worklog, JiraWorklogIssueKey = worklog is null ? null : key, SyncPending = pending, DeletePending = delete,
        };
        await _h.Store.Collection<TimeEntry>().UpsertAsync(e);
        return e;
    }

    private async Task<IReadOnlyList<string>> Left(UserKey owner) =>
        (await _h.Store.Collection<TimeEntry>().QueryAsync(e => e.Owner == owner.Id)).Select(e => e.IssueKey).Order().ToList();

    [Fact]
    public async Task Entries_Jira_has_are_dropped_and_the_rest_stay_as_the_retry_queue()
    {
        await Stored("IN-1", TestUsers.Carl, "101");
        await Stored("CHANGED-1", TestUsers.Carl, "102", pending: true);
        await Stored("NEW-1", TestUsers.Carl, null, pending: true);
        await Stored("GONE-1", TestUsers.Carl, "103", pending: true, delete: true);

        var dropped = await _h.Legacy.RetireSyncedEntriesAsync(TestUsers.Carl);

        Assert.Equal(1, dropped);
        Assert.Equal(["CHANGED-1", "GONE-1", "NEW-1"], await Left(TestUsers.Carl));
        var queue = await _h.Queue.GetPendingAsync(Day, Day);
        Assert.Equal(3, queue.Count);
    }

    [Fact]
    public async Task All_the_users_entries_are_backed_up_first()
    {
        await Stored("IN-1", TestUsers.Carl, "101");
        await Stored("NEW-1", TestUsers.Carl, null, pending: true);
        await Stored("ANNA-1", TestUsers.Anna, "201");

        await _h.Legacy.RetireSyncedEntriesAsync(TestUsers.Carl);

        var backup = Assert.Single(await _h.Store.Collection<TimeEntriesBackup>().QueryAsync(_ => true));
        Assert.Equal(TestUsers.Carl.Id, backup.Owner);
        Assert.Equal(["IN-1", "NEW-1"], backup.Entries.Select(e => e.IssueKey).Order());
        Assert.Equal(["ANNA-1"], await Left(TestUsers.Anna)); // other users are untouched
    }

    [Fact]
    public async Task Running_it_again_changes_nothing()
    {
        await Stored("IN-1", TestUsers.Carl, "101");
        await Stored("NEW-1", TestUsers.Carl, null, pending: true);
        await _h.Legacy.RetireSyncedEntriesAsync(TestUsers.Carl);

        Assert.Equal(0, await _h.Legacy.RetireSyncedEntriesAsync(TestUsers.Carl));
        Assert.Single(await _h.Store.Collection<TimeEntriesBackup>().QueryAsync(_ => true));
        Assert.Equal(["NEW-1"], await Left(TestUsers.Carl));
    }
}
