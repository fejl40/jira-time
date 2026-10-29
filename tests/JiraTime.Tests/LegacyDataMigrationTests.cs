using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Storage;

namespace JiraTime.Tests;

/// <summary>Data saved before multi-user support goes to its old user, after a backup.</summary>
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

        var entries = await StoredAsync(_h.Store, CurrentUser.SignedIn(TestUsers.Carl));
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

        Assert.Empty(await StoredAsync(_h.Store, CurrentUser.SignedIn(TestUsers.Anna)));
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
        Assert.Equal(2, (await StoredAsync(_h.Store, CurrentUser.SignedIn(TestUsers.Carl))).Count);
    }

    [Fact]
    public async Task Without_a_recorded_profile_the_first_user_to_sign_in_gets_it()
    {
        await WriteOldDataAsync(profileId: null);

        Assert.True(await _h.Legacy.ClaimAsync(TestUsers.Anna));
        Assert.Equal(2, (await StoredAsync(_h.Store, CurrentUser.SignedIn(TestUsers.Anna))).Count);
    }

    [Fact]
    public async Task The_data_survives_a_restart_after_moving()
    {
        await WriteOldDataAsync();
        await _h.Legacy.ClaimAsync(TestUsers.Carl);

        var reopened = _h.Files.Reopen();
        Assert.Equal(2, (await StoredAsync(reopened, CurrentUser.SignedIn(TestUsers.Carl))).Count);
        Assert.False(File.Exists(Path.Combine(_h.Files.Path, "days-off", "2026-10-20.json")));
    }

    [Fact]
    public async Task Signing_in_moves_the_old_data_to_the_verified_user()
    {
        await WriteOldDataAsync();
        _h.Jira.Gets["/rest/api/2/myself"] = _ => new { key = "JIRAUSER1", name = "carl", displayName = "Carl Lilholm" };
        var user = new CurrentUser();

        await _h.Identity(user).SignInAsync(TimeHarness.Server);

        // The entry Jira has is dropped after a backup; the one it doesn't have stays to be sent.
        Assert.Equal(["ABC-2"], (await StoredAsync(_h.Store, user)).Select(e => e.IssueKey));
        Assert.Equal(2, Assert.Single(await _h.Store.Collection<TimeEntriesBackup>().QueryAsync(_ => true)).Entries.Count);
    }
}
