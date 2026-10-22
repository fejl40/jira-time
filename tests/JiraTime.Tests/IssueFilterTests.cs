using JiraTime.Domain;

namespace JiraTime.Tests;

public class IssueFilterTests
{
    private static readonly JiraIssue[] Issues =
    [
        new("OPS-7", "Meetings and admin"),
        new("ABC-101", "Payment retries"),
        new("ABC-10", "Login page"),
        new("XYZ-3", "ABC migration notes"),
    ];

    private static string Keys(string? query) => string.Join(" ", IssueFilter.Filter(Issues, query).Select(i => i.Key));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void An_empty_query_keeps_everything_in_order(string? query) =>
        Assert.Equal("OPS-7 ABC-101 ABC-10 XYZ-3", Keys(query));

    [Fact]
    public void Exact_key_first_then_keys_starting_with_the_query() =>
        Assert.Equal("ABC-10 ABC-101", Keys("abc-10"));

    [Fact]
    public void Key_prefixes_rank_before_summary_matches() =>
        Assert.Equal("ABC-101 ABC-10 XYZ-3", Keys("abc"));

    [Fact]
    public void Every_word_must_match_the_key_or_summary() =>
        Assert.Equal("ABC-101", Keys("pay ABC"));

    [Fact]
    public void No_match_gives_nothing() => Assert.Equal("", Keys("nothing like it"));

    [Theory]
    [InlineData(null, false)]
    [InlineData("a", false)]
    [InlineData(" a ", false)]
    [InlineData("ab", true)]
    public void Jira_is_searched_from_two_characters(string? query, bool search) =>
        Assert.Equal(search, IssueFilter.ShouldSearchJira(query));
}
