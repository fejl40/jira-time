using System.Net;
using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Storage.FileSystem;

namespace JiraTime.Tests;

public class JiraSetupTests : IDisposable
{
    private static readonly JiraConnection Server = new("https://jira.example.com", JiraAuthType.Pat, "token", null);
    private static readonly JiraConnection Cloud = new("https://x.atlassian.net", JiraAuthType.Cloud, "token", "me@example.com");

    private readonly TempDir _dir = new();
    private readonly FileDocumentStore _store;
    private readonly SettingsService _settings;
    private readonly FakeJira _jira = new();
    private readonly JiraSetup _setup;

    public JiraSetupTests()
    {
        _store = new FileDocumentStore(new FileDocumentStoreOptions { RootPath = _dir.Path });
        _settings = new SettingsService(_store);
        _setup = new JiraSetup(new JiraClient(_jira.Client()), _settings);
        _jira.Gets["/rest/api/2/myself"] = _ => new { key = "JIRAUSER1", name = "carl", displayName = "Carl Lilholm", emailAddress = "carl@example.com" };
        _jira.Gets["/rest/api/3/myself"] = _ => new { accountId = "5b10ac8d", displayName = "Carl Lilholm", emailAddress = "me@example.com" };
    }

    public void Dispose()
    {
        _store.Dispose();
        _dir.Dispose();
    }

    [Fact]
    public async Task Connecting_saves_the_tokens_own_user_as_the_profile()
    {
        var me = await _setup.ConnectAsync(Server);

        var profile = (await _settings.GetAsync()).Profile;
        Assert.Equal(me, profile);
        Assert.Equal("carl", profile!.Username);
        Assert.Equal("Carl Lilholm", profile.DisplayName);
    }

    [Fact]
    public async Task Cloud_uses_the_account_id()
    {
        await _setup.ConnectAsync(Cloud);

        var profile = (await _settings.GetAsync()).Profile!;
        Assert.Equal("5b10ac8d", profile.Id);
        Assert.Null(profile.Username);
    }

    [Fact]
    public async Task Reconnecting_with_another_token_switches_the_profile()
    {
        await _setup.ConnectAsync(Server);
        _jira.Gets["/rest/api/2/myself"] = _ => new { key = "JIRAUSER2", name = "anna", displayName = "Anna Berg" };

        await _setup.ConnectAsync(Server with { Token = "other" });

        Assert.Equal("anna", (await _settings.GetAsync()).Profile!.Username);
    }

    [Fact]
    public async Task A_bad_token_throws_and_keeps_the_old_profile()
    {
        await _setup.ConnectAsync(Server);
        _jira.FailWith = HttpStatusCode.Unauthorized;

        await Assert.ThrowsAsync<JiraException>(() => _setup.ConnectAsync(Server with { Token = "bad" }));

        Assert.Equal("carl", (await _settings.GetAsync()).Profile!.Username);
    }
}
