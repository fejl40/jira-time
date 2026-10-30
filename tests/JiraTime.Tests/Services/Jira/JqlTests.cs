using JiraTime.Domain;
using JiraTime.Services.Jira;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Services.Jira;

public class JqlTests
{
    [Theory]
    [InlineData("carl", "\"carl\"")]
    [InlineData("say \"hi\"", "\"say \\\"hi\\\"\"")]
    [InlineData(@"DOMAIN\carl", "\"DOMAIN\\\\carl\"")]
    [InlineData(@"end\", "\"end\\\\\"")]
    [InlineData("", "\"\"")]
    public void Strings_are_quoted_and_escaped(string value, string expected) =>
        Assert.Equal(expected, Jql.Quote(value));

    [Fact]
    public void Dates_are_quoted_iso_dates() =>
        Assert.Equal("\"2026-10-09\"", Jql.Date(new DateOnly(2026, 10, 9)));

    [Fact]
    public void User_is_the_username_on_server_and_the_account_id_on_cloud()
    {
        Assert.Equal("\"carl\"", Jql.User(TestUsers.CarlsProfile));
        Assert.Equal("\"5b10ac8d\"", Jql.User(new JiraProfile("5b10ac8d", null, "Carl", null)));
    }

    [Fact]
    public void Lists_are_quoted_and_comma_separated() =>
        Assert.Equal("(\"ABC-1\", \"ABC-2\")", Jql.List(["ABC-1", "ABC-2"]));
}
