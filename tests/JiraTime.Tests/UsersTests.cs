using JiraTime.Domain;

namespace JiraTime.Tests;

/// <summary>Who a user is: a Jira host plus the user id it returns, and the tokens Jira confirmed for them.</summary>
public class UsersTests
{
    private static readonly JiraProfile CarlsProfile = new("JIRAUSER1", "carl", "Carl Lilholm", null);

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

    [Fact]
    public void Confirming_takes_Jiras_profile_and_puts_the_token_first()
    {
        var account = new UserAccount { VerifiedTokens = ["a", "b"] };
        var now = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

        account.Confirm(CarlsProfile, "b", now);

        Assert.Equal(["b", "a"], account.VerifiedTokens);
        Assert.Equal(CarlsProfile, account.Profile);
        Assert.Equal(now, account.LastVerifiedUtc);
    }

    [Fact]
    public void Confirming_keeps_only_the_latest_tokens()
    {
        var account = new UserAccount();

        for (var i = 0; i < UserAccount.RememberedTokens + 2; i++)
            account.Confirm(CarlsProfile, $"t{i}", DateTime.UnixEpoch);

        Assert.Equal(UserAccount.RememberedTokens, account.VerifiedTokens.Count);
        Assert.Equal($"t{UserAccount.RememberedTokens + 1}", account.VerifiedTokens[0]);
        Assert.DoesNotContain("t0", account.VerifiedTokens);
    }

    [Fact]
    public void A_new_account_has_no_times_until_they_are_given()
    {
        var account = new UserAccount();

        Assert.Equal((default(DateTime), default(DateTime)), (account.CreatedUtc, account.LastVerifiedUtc));
    }
}
