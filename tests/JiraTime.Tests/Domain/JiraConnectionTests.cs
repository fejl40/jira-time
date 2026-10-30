using JiraTime.Domain;

namespace JiraTime.Tests.Domain;

/// <summary>The Jira URL as typed on Connect.</summary>
public class JiraConnectionTests
{
    [Theory]
    [InlineData("jira.example.com", "https://jira.example.com")]
    [InlineData("  https://jira.example.com/  ", "https://jira.example.com")]
    [InlineData("https://acme.atlassian.net//", "https://acme.atlassian.net")]
    [InlineData("http://jira.local:8080/jira/", "http://jira.local:8080/jira")]
    [InlineData("HTTPS://Jira.Example.com", "HTTPS://Jira.Example.com")]
    public void A_typed_Jira_url_is_trimmed_and_gets_https_when_it_has_no_scheme(string typed, string expected) =>
        Assert.Equal(expected, JiraConnection.NormalizeUrl(typed));

    [Theory]
    [InlineData("not a url")]
    [InlineData("jira.example.com:port")]
    [InlineData("https://")]
    [InlineData("http://")]
    [InlineData("https:///")]
    [InlineData("  https://  ")]
    [InlineData("ftp://jira.example.com")]
    [InlineData("")]
    public void Text_that_is_not_a_url_is_rejected(string typed) =>
        Assert.Null(JiraConnection.NormalizeUrl(typed));
}
