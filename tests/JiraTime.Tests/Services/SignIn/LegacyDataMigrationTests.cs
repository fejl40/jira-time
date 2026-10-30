using JiraTime.Domain;
using JiraTime.Storage;
using static JiraTime.Tests.Support.TestEntries;
using JiraTime.Services.SignIn;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Services.SignIn;

/// <summary>
/// Data saved before multi-user support goes to its old user, and entries from when the app kept logged time itself are
/// dropped once Jira has them; both after a backup.
/// </summary>
public class LegacyDataMigrationTests : IDisposable
{
    private readonly TimeHarness _h = new();

    public void Dispose() => _h.Dispose();

    /// <summary>Writes data the way the app stored it before multi-user support.</summary>
    private async Task WriteOldDataAsync(string? profileId = "JIRAUSER1")
    {
        void Write(string collection, string id, string json) =>
            File.WriteAllText(Path.Combine(_h.Files.Folder(collection), id + ".json"), json);
        Write("time-entries", "e1", """{ "id": "e1", "issueKey": "ABC-1", "issueSummary": "Login", "date": "2026-10-16", "duration": "08:00:00", "jiraWorklogId": "10", "jiraWorklogIssueKey": "ABC-1", "syncPending": false }""");
        Write("time-entries", "e2", """{ "id": "e2", "issueKey": "ABC-2", "date": "2026-10-19", "start": "09:00:00", "end": "11:00:00", "syncPending": true }""");
        Write("days-off", "2026-10-20", """{ "id": "2026-10-20", "date": "2026-10-20", "reason": "Vacation" }""");
        var profile = profileId is null ? "null" : $$"""{ "id": "{{profileId}}", "username": "carl", "displayName": "Carl Lilholm" }""";
        Write("settings", "app", $$"""{ "id": "app", "profile": {{profile}}, "workday": { "start": "08:00:00" } }""");
        await Task.CompletedTask;
    }

    private static readonly DateOnly From = new(2026, 10, 1), To = new(2026, 10, 31);

    /// <summary>All the user's stored entries, waiting for Jira or not, in day order.</summary>
    private static async Task<IReadOnlyList<TimeEntry>> StoredAsync(FileDocumentStore store, CurrentUser user)
    {
        var owner = user.Owner;
        return (await store.Collection<TimeEntry>().QueryAsync(e => e.Owner == owner && e.Date >= From && e.Date <= To)).OrderBy(e => e.Date).ToList();
    }

    [Fact]
    public async Task The_old_user_gets_all_old_data_after_a_backup()
    {
        await WriteOldDataAsync();

        Assert.True(await _h.Legacy.ClaimAsync(TestUsers.Carl));

        var entries = await StoredAsync(_h.Store, TestUsers.SignedIn(TestUsers.Carl));
        Assert.Equal(["ABC-1", "ABC-2"], entries.Select(e => e.IssueKey));
        Assert.Equal(TimeSpan.FromHours(2), entries[1].Duration);       // old start/end kept as hours
        Assert.Equal("10", entries[0].JiraWorklogId);                    // still linked to its Jira worklog
        Assert.True(entries[0].IsSynced);

        var calendar = _h.Calendar;
        Assert.Equal(DayKind.DayOff, (await calendar.GetDayAsync(new DateOnly(2026, 10, 20))).Kind);

        var backup = Assert.Single(await _h.Store.Collection<LegacyBackup>().QueryAsync(_ => true));
        Assert.Equal(2, backup.Entries.Count);
        Assert.All(backup.Entries, e => Assert.Null(e.Owner));
        Assert.Single(backup.DaysOff);
        Assert.Equal("JIRAUSER1", backup.Settings?.Profile?.Id);
        Assert.Equal(TestUsers.Carl, backup.ClaimedBy);
    }

    [Fact]
    public async Task Another_user_does_not_get_the_old_data()
    {
        await WriteOldDataAsync();

        Assert.False(await _h.Legacy.ClaimAsync(TestUsers.Anna));

        Assert.Empty(await StoredAsync(_h.Store, TestUsers.SignedIn(TestUsers.Anna)));
        Assert.Empty(await _h.Store.Collection<LegacyBackup>().QueryAsync(_ => true));
        Assert.True(await _h.Legacy.ClaimAsync(TestUsers.Carl)); // still there for Carl
    }

    [Fact]
    public async Task Running_it_again_changes_nothing()
    {
        await WriteOldDataAsync();
        await _h.Legacy.ClaimAsync(TestUsers.Carl);

        Assert.False(await _h.Legacy.ClaimAsync(TestUsers.Carl));
        Assert.Single(await _h.Store.Collection<LegacyBackup>().QueryAsync(_ => true));
        Assert.Equal(2, (await StoredAsync(_h.Store, TestUsers.SignedIn(TestUsers.Carl))).Count);
    }

    [Fact]
    public async Task Without_a_recorded_profile_the_first_user_to_sign_in_gets_it()
    {
        await WriteOldDataAsync(profileId: null);

        Assert.True(await _h.Legacy.ClaimAsync(TestUsers.Anna));
        Assert.Equal(2, (await StoredAsync(_h.Store, TestUsers.SignedIn(TestUsers.Anna))).Count);
    }

    [Fact]
    public async Task The_data_survives_a_restart_after_moving()
    {
        await WriteOldDataAsync();
        await _h.Legacy.ClaimAsync(TestUsers.Carl);

        var reopened = _h.Files.Reopen();
        Assert.Equal(2, (await StoredAsync(reopened, TestUsers.SignedIn(TestUsers.Carl))).Count);
        Assert.False(File.Exists(Path.Combine(_h.Files.Path, "days-off", "2026-10-20.json")));
    }

    [Fact]
    public async Task Signing_in_moves_the_old_data_to_the_verified_user()
    {
        await WriteOldDataAsync();
        var user = new CurrentUser();

        await _h.Identity(user).SignInAsync(TimeHarness.Server);

        // The entry Jira has is dropped after a backup; the one it doesn't have stays to be sent.
        Assert.Equal(["ABC-2"], (await StoredAsync(_h.Store, user)).Select(e => e.IssueKey));
        Assert.Equal(2, Assert.Single(await _h.Store.Collection<TimeEntriesBackup>().QueryAsync(_ => true)).Entries.Count);
    }

    [Fact]
    public async Task Owned_entries_saved_with_start_and_end_times_get_their_hours_on_sign_in()
    {
        var folder = _h.Files.Folder("time-entries");
        await File.WriteAllTextAsync(Path.Combine(folder, "old.json"), $$"""
            { "id": "old", "owner": "{{TestUsers.Carl.Id}}", "issueKey": "ABC-1", "issueSummary": "", "date": "2026-10-19",
              "start": "09:00:00", "end": "11:30:00", "description": "Review", "syncPending": true }
            """);

        Assert.False(await _h.Legacy.ClaimAsync(TestUsers.Carl)); // nothing unowned to claim

        var old = Assert.Single(await StoredAsync(_h.Store, TestUsers.SignedIn(TestUsers.Carl)));
        Assert.Equal((TimeSpan.FromHours(2.5), "Review"), (old.Duration, old.Comment));
        var json = await File.ReadAllTextAsync(Path.Combine(folder, "old.json"));
        Assert.DoesNotContain("\"start\"", json);
        Assert.Contains("\"duration\"", json);
    }

    [Fact]
    public async Task Start_and_end_times_never_replace_hours_an_entry_already_has()
    {
        var folder = _h.Files.Folder("time-entries");
        await File.WriteAllTextAsync(Path.Combine(folder, "both.json"), $$"""
            { "id": "both", "owner": "{{TestUsers.Carl.Id}}", "issueKey": "ABC-1", "date": "2026-10-19", "duration": "01:00:00",
              "start": "09:00:00", "end": "11:30:00", "syncPending": true }
            """);

        await _h.Legacy.ClaimAsync(TestUsers.Carl);

        Assert.Equal(TimeSpan.FromHours(1), Assert.Single(await StoredAsync(_h.Store, TestUsers.SignedIn(TestUsers.Carl))).Duration);
    }

    // ---- Entries from when the app kept logged time itself: dropped once Jira has them, after a backup ----

    private static readonly DateOnly Day = new(2026, 10, 19);

    private async Task StoreAsync(UserKey owner, TimeEntry entry)
    {
        entry.Owner = owner.Id;
        await _h.Store.Collection<TimeEntry>().UpsertAsync(entry);
    }

    private async Task<IReadOnlyList<string>> Left(UserKey owner) =>
        (await _h.Store.Collection<TimeEntry>().QueryAsync(e => e.Owner == owner.Id)).Select(e => e.IssueKey).Order().ToList();

    [Fact]
    public async Task Entries_Jira_has_are_dropped_and_the_rest_stay_as_the_retry_queue()
    {
        await StoreAsync(TestUsers.Carl, Saved("IN-1", 1, Day, worklog: "101"));
        var changed = Saved("CHANGED-1", 1, Day, worklog: "102");
        changed.Failed("Jira was down");
        await StoreAsync(TestUsers.Carl, changed);
        await StoreAsync(TestUsers.Carl, Waiting("NEW-1", 1, Day));
        var gone = Saved("GONE-1", 1, Day, worklog: "103");
        gone.MarkRemovalPending();
        await StoreAsync(TestUsers.Carl, gone);

        var dropped = await _h.Legacy.RetireSyncedEntriesAsync(TestUsers.Carl);

        Assert.Equal(1, dropped);
        Assert.Equal(["CHANGED-1", "GONE-1", "NEW-1"], await Left(TestUsers.Carl));
        var queue = await _h.Queue.GetPendingAsync(Day, Day);
        Assert.Equal(3, queue.Count);
    }

    [Fact]
    public async Task All_the_users_entries_are_backed_up_first()
    {
        await StoreAsync(TestUsers.Carl, Saved("IN-1", 1, Day, worklog: "101"));
        await StoreAsync(TestUsers.Carl, Waiting("NEW-1", 1, Day));
        await StoreAsync(TestUsers.Anna, Saved("ANNA-1", 1, Day, worklog: "201"));

        await _h.Legacy.RetireSyncedEntriesAsync(TestUsers.Carl);

        var backup = Assert.Single(await _h.Store.Collection<TimeEntriesBackup>().QueryAsync(_ => true));
        Assert.Equal(TestUsers.Carl.Id, backup.Owner);
        Assert.Equal(["IN-1", "NEW-1"], backup.Entries.Select(e => e.IssueKey).Order());
        Assert.Equal(["ANNA-1"], await Left(TestUsers.Anna)); // other users are untouched
    }

    [Fact]
    public async Task Retiring_entries_again_changes_nothing()
    {
        await StoreAsync(TestUsers.Carl, Saved("IN-1", 1, Day, worklog: "101"));
        await StoreAsync(TestUsers.Carl, Waiting("NEW-1", 1, Day));
        await _h.Legacy.RetireSyncedEntriesAsync(TestUsers.Carl);

        Assert.Equal(0, await _h.Legacy.RetireSyncedEntriesAsync(TestUsers.Carl));
        Assert.Single(await _h.Store.Collection<TimeEntriesBackup>().QueryAsync(_ => true));
        Assert.Equal(["NEW-1"], await Left(TestUsers.Carl));
    }
}
