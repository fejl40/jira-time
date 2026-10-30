using JiraTime.Domain.JiraLog;

namespace JiraTime.Tests.Domain.JiraLog;

/// <summary>Logged bodies as the log shows them: JSON trees that start partly open, or raw text with a note.</summary>
public class JsonBodyTests
{
    private const string Search = """{"total":2,"issues":[{"key":"ABC-1","fields":{"summary":"One","status":{"name":"Done"}}},{"key":"ABC-2"}]}""";

    [Fact]
    public void JSON_reads_as_a_tree_whose_top_two_levels_start_open()
    {
        var body = JsonBody.Read(Search, truncated: false)!;

        Assert.True(body.IsTree);
        Assert.Null(body.Note);
        Assert.Equal(["$", "$.issues"], body.OpenAtStart().Order());
    }

    [Fact]
    public void Expand_all_opens_every_object_and_array()
    {
        var all = JsonBody.Read(Search, truncated: false)!.All();

        Assert.Equal(["$", "$.issues", "$.issues[0]", "$.issues[0].fields", "$.issues[0].fields.status", "$.issues[1]"], all.Order());
    }

    [Fact]
    public void A_body_that_is_not_JSON_is_raw_text_with_a_note()
    {
        var body = JsonBody.Read("<html>Log in</html>", truncated: false)!;

        Assert.False(body.IsTree);
        Assert.Contains("not JSON", body.Note);
        Assert.Equal("<html>Log in</html>", body.Pretty);
    }

    [Fact]
    public void A_truncated_body_is_raw_text_even_when_it_starts_like_JSON()
    {
        var body = JsonBody.Read("""{"issues":[{"key":""", truncated: true)!;

        Assert.False(body.IsTree);
        Assert.Contains("cut at 64 KB", body.Note);
    }

    [Fact]
    public void No_body_reads_as_none() => Assert.Null(JsonBody.Read("", truncated: false));

    [Fact]
    public void Folded_nodes_say_what_they_hold_and_children_are_labelled()
    {
        var root = JsonBody.Read(Search, truncated: false)!.Root!;

        Assert.Equal("2 keys", JsonBody.Summary(root));
        Assert.Equal("2 items", JsonBody.Summary(root["issues"]!));
        Assert.Equal(["\"total\"", "\"issues\""], JsonBody.Children(root).Select(c => c.Label));
        Assert.Equal(["0", "1"], JsonBody.Children(root["issues"]!).Select(c => c.Label));
    }

    [Fact]
    public void Pretty_indents_JSON_for_copying() =>
        Assert.Contains("\n  \"total\": 2", JsonBody.Read(Search, truncated: false)!.Pretty);
}
