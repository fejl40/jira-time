using System.Net;
using JiraTime.Domain;
using JiraTime.Services;

namespace JiraTime.Tests;

/// <summary>Signing in as the user Jira says the token belongs to, and offline with a token Jira confirmed before.</summary>
public class JiraIdentityTests : IDisposable
{
    private static readonly JiraConnection Server = TimeHarness.Server;

    private readonly TimeHarness _h = new();
    private readonly CurrentUser _user = new();
    private readonly JiraIdentity _identity;

    public JiraIdentityTests() => _identity = _h.Identity(_user);

    public void Dispose() => _h.Dispose();

    [Fact]
    public async Task Signing_in_uses_the_user_Jira_returns_for_the_token()
    {
        var result = await _identity.SignInAsync(Server);

        Assert.True(result.SignedIn);
        Assert.Equal(TestUsers.Carl, _user.Key);
        Assert.Equal("Carl Lilholm", _user.Jira!.Profile.DisplayName);
        Assert.False(_user.VerifiedOffline);
        var account = await _h.Store.Collection<UserAccount>().GetAsync(TestUsers.Carl.Id);
        Assert.NotNull(account);
        Assert.Contains(UserAccount.TokenHash(Server), account.VerifiedTokens);
    }

    [Fact]
    public async Task Another_token_on_the_same_host_is_another_user()
    {
        await _identity.SignInAsync(Server);
        _h.Jira.Myself = new { key = "JIRAUSER2", name = "anna", displayName = "Anna Berg" };

        await _identity.SignInAsync(Server with { Token = "token-2" });

        Assert.Equal(TestUsers.Anna, _user.Key);
    }

    [Fact]
    public async Task The_same_user_id_on_another_host_is_another_user()
    {
        await _identity.SignInAsync(Server with { BaseUrl = "https://jira.other.com" });

        Assert.Equal("jira.other.com", _user.Key!.Host);
        Assert.NotEqual(TestUsers.Carl.Id, _user.Owner);
    }

    [Fact]
    public async Task A_rejected_token_is_not_signed_in_and_is_forgotten()
    {
        await _identity.SignInAsync(Server);
        _h.Jira.FailWith = HttpStatusCode.Unauthorized;

        var result = await _identity.SignInAsync(Server);

        Assert.False(result.SignedIn);
        Assert.True(result.Rejected);
        Assert.False(_user.IsSignedIn);

        // Not even offline afterwards.
        _h.Jira.FailWith = null;
        _h.Jira.Offline = true;
        Assert.False((await _identity.SignInAsync(Server)).SignedIn);
    }

    [Theory]
    [InlineData(false)] // network error
    [InlineData(true)]  // server error
    public async Task When_Jira_is_unreachable_a_token_confirmed_before_still_works(bool serverError)
    {
        await _identity.SignInAsync(Server);
        _user.Clear();
        if (serverError) _h.Jira.FailWith = HttpStatusCode.ServiceUnavailable;
        else _h.Jira.Offline = true;

        var result = await _identity.SignInAsync(Server);

        Assert.True(result.SignedIn);
        Assert.True(_user.VerifiedOffline);
        Assert.Equal(TestUsers.Carl, _user.Key);
    }

    [Fact]
    public async Task When_Jira_is_unreachable_an_unknown_token_is_refused()
    {
        await _identity.SignInAsync(Server);
        _h.Jira.Offline = true;

        var result = await _identity.SignInAsync(Server with { Token = "made-up" });

        Assert.False(result.SignedIn);
        Assert.False(result.Rejected);
        Assert.False(_user.IsSignedIn);
    }

    [Fact]
    public async Task A_new_connection_needs_Jira_to_confirm_it()
    {
        await _identity.SignInAsync(Server);
        _h.Jira.Offline = true;

        Assert.False((await _identity.SignInAsync(Server, allowOffline: false)).SignedIn);
    }

    [Fact]
    public async Task Only_the_latest_tokens_are_remembered()
    {
        for (var i = 0; i < UserAccount.RememberedTokens + 2; i++)
            await _identity.SignInAsync(Server with { Token = $"t{i}" });

        var account = (await _h.Store.Collection<UserAccount>().GetAsync(TestUsers.Carl.Id))!;
        Assert.Equal(UserAccount.RememberedTokens, account.VerifiedTokens.Count);
        Assert.Equal(UserAccount.TokenHash(Server with { Token = $"t{UserAccount.RememberedTokens + 1}" }), account.VerifiedTokens[0]);
    }
}
