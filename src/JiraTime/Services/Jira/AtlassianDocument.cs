using System.Text.Json;
using JiraTime.Domain;

namespace JiraTime.Services.Jira;

/// <summary>Converts between plain text and Atlassian Document Format (ADF), which Cloud uses for comments.</summary>
public static class AtlassianDocument
{
    /// <summary>
    /// Plain text of a comment: a string on Server/DC, an ADF document on Cloud.
    /// Paragraphs become lines. Cleaned as a typed comment is (<see cref="TimeEntry.CleanComment"/>), so comments
    /// compare one way wherever they came from: trimmed, and null when there is no text.
    /// </summary>
    public static string? ToPlainText(JsonElement? comment)
    {
        if (comment is not { } c) return null;
        string? text = c.ValueKind switch
        {
            JsonValueKind.String => c.GetString(),
            JsonValueKind.Object => string.Join("\n", Paragraphs(c)),
            _ => null,
        };
        return TimeEntry.CleanComment(text);

        static IEnumerable<string> Paragraphs(JsonElement doc) =>
            doc.TryGetProperty("content", out var blocks) && blocks.ValueKind == JsonValueKind.Array
                ? blocks.EnumerateArray().Select(b => string.Concat(Texts(b))).Where(t => t.Length > 0)
                : [];

        static IEnumerable<string> Texts(JsonElement node)
        {
            if (node.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String) yield return t.GetString()!;
            if (node.TryGetProperty("content", out var kids) && kids.ValueKind == JsonValueKind.Array)
                foreach (var kid in kids.EnumerateArray())
                foreach (var s in Texts(kid))
                    yield return s;
        }
    }

    /// <summary>An ADF document holding <paramref name="text"/> as one paragraph (no content when empty).</summary>
    public static object FromPlainText(string text) => new
    {
        type = "doc",
        version = 1,
        content = string.IsNullOrEmpty(text)
            ? Array.Empty<object>()
            : [new { type = "paragraph", content = new[] { new { type = "text", text } } }],
    };
}
