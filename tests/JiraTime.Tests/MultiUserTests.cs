using System.Net;
using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Storage.FileSystem;

namespace JiraTime.Tests;

public class UserKeyTests
{
    [Theory]
    [InlineData("https://jira.example.com", "jira.example.com")]
    [InlineData("https://JIRA.example.com/", "jira.example.com")]
    [InlineData("http://jira.example.com", "jira.example.com")]
    [InlineData("https://jira.example.com:8443/jira/", "jira.example.com:8443/jira")]
    [InlineData("https://x.atlassian.net", "x.atlassian.net")]
    public void Hosts_are_normalized(string url, string host) => Assert.Equal(host, UserKey.NormalizeHost(url));

    [Fact]
    public void The_owner_id_depends_on_both_host_and_user_id()
    {
        var a = UserKey.For("https://jira.example.com/", "JIRAUSER1");

        Assert.Equal(a.Id, UserKey.For("https://JIRA.example.com", "JIRAUSER1").Id);
        Assert.NotEqual(a.Id, UserKey.For("https://jira.other.com", "JIRAUSER1").Id);
        Assert.NotEqual(a.Id, UserKey.For("https://jira.example.com", "JIRAUSER2").Id);
        Assert.Matches("^[0-9a-f]{24}$", a.Id);
    }
}

public class JiraIdentityTests : IDisposable
{
    private static readonly JiraConnection Server = new("https://jira.example.com", JiraAuthType.Pat, "token-1", null);

    private readonly TempDir _dir = new();
    private readonly FileDocumentStore _store;
    private readonly FakeJira _jira = new();
    private readonly CurrentUser _user = new();
    private readonly JiraIdentity _identity;

    public JiraIdentityTests()
    {
        _store = new FileDocumentStore(new FileDocumentStoreOptions { RootPath = _dir.Path });
        _identity = new JiraIdentity(new JiraClient(_jira.Client()), _store, _user, new LegacyDataMigration(_store));
        Me("JIRAUSER1", "carl", "Carl Lilholm");
    }

    public void Dispose()
    {
        _store.Dispose();
        _dir.Dispose();
    }

    private void Me(string key, string name, string display) =>
        _jira.Gets["/rest/api/2/myself"] = _ => new { key, name, displayName = display };

    [Fact]
    public async Task Signing_in_uses_the_user_Jira_returns_for_the_token()
    {
        var result = await _identity.SignInAsync(Server);

        Assert.True(result.SignedIn);
        Assert.Equal(TestUsers.Carl, _user.Key);
        Assert.Equal("Carl Lilholm", _user.Profile!.DisplayName);
        Assert.False(_user.VerifiedOffline);
        var account = await _store.Collection<UserAccount>().GetAsync(TestUsers.Carl.Id);
        Assert.NotNull(account);
        Assert.Contains(UserAccount.TokenHash(Server), account.VerifiedTokens);
    }

    [Fact]
    public async Task Another_token_on_the_same_host_is_another_user()
    {
        await _identity.SignInAsync(Server);
        Me("JIRAUSER2", "anna", "Anna Berg");

        await _identity.SignInAsync(Server with { Token = "token-2" });

        Assert.Equal(TestUsers.Anna, _user.Key);
    }

    [Fact]
    public async Task The_same_user_id_on_another_host_is_another_user()
    {
        _jira.Gets["/rest/api/2/myself"] = _ => new { key = "JIRAUSER1", name = "carl", displayName = "Carl" };

        await _identity.SignInAsync(Server with { BaseUrl = "https://jira.other.com" });

        Assert.Equal("jira.other.com", _user.Key!.Host);
        Assert.NotEqual(TestUsers.Carl.Id, _user.Owner);
    }

    [Fact]
    public async Task A_rejected_token_is_not_signed_in_and_is_forgotten()
    {
        await _identity.SignInAsync(Server);
        _jira.FailWith = HttpStatusCode.Unauthorized;

        var result = await _identity.SignInAsync(Server);

        Assert.False(result.SignedIn);
        Assert.True(result.Rejected);
        Assert.False(_user.IsSignedIn);

        // Not even offline afterwards.
        _jira.FailWith = null;
        _jira.Offline = true;
        Assert.False((await _identity.SignInAsync(Server)).SignedIn);
    }

    [Theory]
    [InlineData(false)] // network error
    [InlineData(true)]  // server error
    public async Task When_Jira_is_unreachable_a_token_confirmed_before_still_works(bool serverError)
    {
        await _identity.SignInAsync(Server);
        _user.Clear();
        if (serverError) _jira.FailWith = HttpStatusCode.ServiceUnavailable;
        else _jira.Offline = true;

        var result = await _identity.SignInAsync(Server);

        Assert.True(result.SignedIn);
        Assert.True(_user.VerifiedOffline);
        Assert.Equal(TestUsers.Carl, _user.Key);
    }

    [Fact]
    public async Task When_Jira_is_unreachable_an_unknown_token_is_refused()
    {
        await _identity.SignInAsync(Server);
        _jira.Offline = true;

        var result = await _identity.SignInAsync(Server with { Token = "made-up" });

        Assert.False(result.SignedIn);
        Assert.False(result.Rejected);
        Assert.False(_user.IsSignedIn);
    }

    [Fact]
    public async Task A_new_connection_needs_Jira_to_confirm_it()
    {
        await _identity.SignInAsync(Server);
        _jira.Offline = true;

        Assert.False((await _identity.SignInAsync(Server, allowOffline: false)).SignedIn);
    }

    [Fact]
    public async Task Only_the_latest_tokens_are_remembered()
    {
        for (var i = 0; i < JiraIdentity.RememberedTokens + 2; i++)
            await _identity.SignInAsync(Server with { Token = $"t{i}" });

        var account = (await _store.Collection<UserAccount>().GetAsync(TestUsers.Carl.Id))!;
        Assert.Equal(JiraIdentity.RememberedTokens, account.VerifiedTokens.Count);
        Assert.Equal(UserAccount.TokenHash(Server with { Token = $"t{JiraIdentity.RememberedTokens + 1}" }), account.VerifiedTokens[0]);
    }
}

public class DataSeparationTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 19);

    private readonly TempDir _dir = new();
    private readonly FileDocumentStore _store;

    public DataSeparationTests() => _store = new FileDocumentStore(new FileDocumentStoreOptions { RootPath = _dir.Path });

    public void Dispose()
    {
        _store.Dispose();
        _dir.Dispose();
    }

    private TimeEntryService Entries(UserKey user) => new(_store, CurrentUser.SignedIn(user));
    private WorkCalendarService Calendar(UserKey user) => new(_store, CurrentUser.SignedIn(user));

    private static TimeEntry Entry(string key, double hours) => new() { IssueKey = key, Date = Day, Duration = TimeSpan.FromHours(hours) };

    [Fact]
    public async Task Users_only_see_their_own_entries()
    {
        await Entries(TestUsers.Carl).SaveAsync(Entry("ABC-1", 8));
        await Entries(TestUsers.Anna).SaveAsync(Entry("XYZ-1", 3));

        Assert.Equal(["ABC-1"], (await Entries(TestUsers.Carl).GetRangeAsync(Day, Day)).Select(e => e.IssueKey));
        Assert.Equal(["XYZ-1"], (await Entries(TestUsers.Anna).GetRangeAsync(Day, Day)).Select(e => e.IssueKey));
        Assert.Single(await Entries(TestUsers.Anna).GetPendingAsync(Day, Day));
    }

    [Fact]
    public async Task Entries_are_stored_with_their_owner()
    {
        var entry = Entry("ABC-1", 8);
        await Entries(TestUsers.Carl).SaveAsync(entry);

        Assert.Equal(TestUsers.Carl.Id, (await _store.Collection<TimeEntry>().GetAsync(entry.Id))!.Owner);
    }

    [Fact]
    public async Task A_user_cannot_load_change_or_delete_someone_elses_entry()
    {
        var carls = Entry("ABC-1", 8);
        await Entries(TestUsers.Carl).SaveAsync(carls);
        var anna = Entries(TestUsers.Anna);

        Assert.Null(await anna.GetAsync(carls.Id));

        var forged = carls.Copy();
        forged.Owner = null;
        forged.Duration = TimeSpan.FromHours(1);
        Assert.NotNull(await anna.SaveAsync(forged));
        await Assert.ThrowsAsync<InvalidOperationException>(() => anna.SaveSyncStateAsync(forged));

        await anna.DeleteAsync(carls.Id);

        var stored = (await Entries(TestUsers.Carl).GetAsync(carls.Id))!;
        Assert.Equal(TimeSpan.FromHours(8), stored.Duration);
    }

    [Fact]
    public async Task The_24_hour_limit_counts_only_the_users_own_hours()
    {
        await Entries(TestUsers.Carl).SaveAsync(Entry("ABC-1", 20));

        Assert.Null(await Entries(TestUsers.Anna).SaveAsync(Entry("XYZ-1", 20)));
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
        var service = new TimeEntryService(_store, new CurrentUser());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetRangeAsync(Day, Day));
    }
}

public class LegacyDataMigrationTests : IDisposable
{
    private readonly TempDir _dir = new();

    public void Dispose() => _dir.Dispose();

    private FileDocumentStore Store() => new(new FileDocumentStoreOptions { RootPath = _dir.Path });

    /// <summary>Writes data the way the app stored it before multi-user support.</summary>
    private async Task WriteOldDataAsync(string? profileId = "JIRAUSER1")
    {
        void Write(string collection, string id, string json)
        {
            Directory.CreateDirectory(Path.Combine(_dir.Path, collection));
            File.WriteAllText(Path.Combine(_dir.Path, collection, id + ".json"), json);
        }
        Write("time-entries", "e1", """{ "id": "e1", "issueKey": "ABC-1", "issueSummary": "Login", "date": "2026-10-16", "duration": "08:00:00", "jiraWorklogId": "10", "jiraWorklogIssueKey": "ABC-1", "syncPending": false }""");
        Write("time-entries", "e2", """{ "id": "e2", "issueKey": "ABC-2", "date": "2026-10-19", "start": "09:00:00", "end": "11:00:00", "syncPending": true }""");
        Write("days-off", "2026-10-20", """{ "id": "2026-10-20", "date": "2026-10-20", "reason": "Vacation" }""");
        var profile = profileId is null ? "null" : $$"""{ "id": "{{profileId}}", "username": "carl", "displayName": "Carl Lilholm" }""";
        Write("settings", "app", $$"""{ "id": "app", "profile": {{profile}}, "workday": { "start": "08:00:00" } }""");
        await Task.CompletedTask;
    }

    private static readonly DateOnly From = new(2026, 10, 1), To = new(2026, 10, 31);

    [Fact]
    public async Task The_old_user_gets_all_old_data_after_a_backup()
    {
        await WriteOldDataAsync();
        using var store = Store();

        Assert.True(await new LegacyDataMigration(store).ClaimAsync(TestUsers.Carl));

        var entries = await new TimeEntryService(store, CurrentUser.SignedIn(TestUsers.Carl)).GetRangeAsync(From, To);
        Assert.Equal(["ABC-1", "ABC-2"], entries.Select(e => e.IssueKey));
        Assert.Equal(TimeSpan.FromHours(2), entries[1].Duration);       // old start/end kept as hours
        Assert.Equal("10", entries[0].JiraWorklogId);                    // still linked to its Jira worklog
        Assert.True(entries[0].IsSynced);

        var calendar = new WorkCalendarService(store, CurrentUser.SignedIn(TestUsers.Carl));
        Assert.Equal(DayKind.DayOff, (await calendar.GetDayAsync(new DateOnly(2026, 10, 20))).Kind);

        var backup = Assert.Single(await store.Collection<LegacyBackup>().QueryAsync(_ => true));
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
        using var store = Store();

        Assert.False(await new LegacyDataMigration(store).ClaimAsync(TestUsers.Anna));

        Assert.Empty(await new TimeEntryService(store, CurrentUser.SignedIn(TestUsers.Anna)).GetRangeAsync(From, To));
        Assert.Empty(await store.Collection<LegacyBackup>().QueryAsync(_ => true));
        Assert.True(await new LegacyDataMigration(store).ClaimAsync(TestUsers.Carl)); // still there for Carl
    }

    [Fact]
    public async Task Running_it_again_changes_nothing()
    {
        await WriteOldDataAsync();
        using var store = Store();
        var migration = new LegacyDataMigration(store);
        await migration.ClaimAsync(TestUsers.Carl);

        Assert.False(await migration.ClaimAsync(TestUsers.Carl));
        Assert.Single(await store.Collection<LegacyBackup>().QueryAsync(_ => true));
        Assert.Equal(2, (await new TimeEntryService(store, CurrentUser.SignedIn(TestUsers.Carl)).GetRangeAsync(From, To)).Count);
    }

    [Fact]
    public async Task Without_a_recorded_profile_the_first_user_to_sign_in_gets_it()
    {
        await WriteOldDataAsync(profileId: null);
        using var store = Store();

        Assert.True(await new LegacyDataMigration(store).ClaimAsync(TestUsers.Anna));
        Assert.Equal(2, (await new TimeEntryService(store, CurrentUser.SignedIn(TestUsers.Anna)).GetRangeAsync(From, To)).Count);
    }

    [Fact]
    public async Task The_data_survives_a_restart_after_moving()
    {
        await WriteOldDataAsync();
        using (var store = Store())
            await new LegacyDataMigration(store).ClaimAsync(TestUsers.Carl);

        using var reopened = Store();
        Assert.Equal(2, (await new TimeEntryService(reopened, CurrentUser.SignedIn(TestUsers.Carl)).GetRangeAsync(From, To)).Count);
        Assert.False(File.Exists(Path.Combine(_dir.Path, "days-off", "2026-10-20.json")));
    }

    [Fact]
    public async Task Signing_in_moves_the_old_data_to_the_verified_user()
    {
        await WriteOldDataAsync();
        using var store = Store();
        var jira = new FakeJira();
        jira.Gets["/rest/api/2/myself"] = _ => new { key = "JIRAUSER1", name = "carl", displayName = "Carl Lilholm" };
        var user = new CurrentUser();

        await new JiraIdentity(new JiraClient(jira.Client()), store, user, new LegacyDataMigration(store))
            .SignInAsync(new JiraConnection("https://jira.example.com", JiraAuthType.Pat, "t", null));

        Assert.Equal(2, (await new TimeEntryService(store, user).GetRangeAsync(From, To)).Count);
    }
}
