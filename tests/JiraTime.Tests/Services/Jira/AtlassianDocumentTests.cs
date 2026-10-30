using System.Text.Json;
using JiraTime.Services;

namespace JiraTime.Tests;

public class AtlassianDocumentTests
{
    private static JsonElement Serialize(object value) => JsonSerializer.SerializeToElement(value);

    [Theory]
    [InlineData("Fixed the login bug")]
    [InlineData("Quotes \" and \\ backslashes")]
    public void Text_survives_a_round_trip(string text) =>
        Assert.Equal(text, AtlassianDocument.ToPlainText(Serialize(AtlassianDocument.FromPlainText(text))));

    [Fact]
    public void Empty_text_is_an_empty_document_and_reads_back_as_null()
    {
        var doc = Serialize(AtlassianDocument.FromPlainText(""));
        Assert.Equal("doc", doc.GetProperty("type").GetString());
        Assert.Equal(1, doc.GetProperty("version").GetInt32());
        Assert.Equal(0, doc.GetProperty("content").GetArrayLength());
        Assert.Null(AtlassianDocument.ToPlainText(doc));
    }

    [Fact]
    public void Text_becomes_one_paragraph_with_one_text_node()
    {
        var paragraph = Assert.Single(Serialize(AtlassianDocument.FromPlainText("Hi")).GetProperty("content").EnumerateArray());
        Assert.Equal("paragraph", paragraph.GetProperty("type").GetString());
        var node = Assert.Single(paragraph.GetProperty("content").EnumerateArray());
        Assert.Equal("text", node.GetProperty("type").GetString());
        Assert.Equal("Hi", node.GetProperty("text").GetString());
    }

    [Fact]
    public void Missing_comment_is_null() => Assert.Null(AtlassianDocument.ToPlainText(null));
}
