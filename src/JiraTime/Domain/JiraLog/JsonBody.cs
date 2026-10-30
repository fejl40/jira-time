using System.Text.Json;
using System.Text.Json.Nodes;

namespace JiraTime.Domain.JiraLog;

/// <summary>
/// A logged request or response body as the log shows it: a JSON tree to fold and unfold, or the raw text
/// with a note why (not JSON, or cut short so it can't be read as JSON).
/// </summary>
public sealed record JsonBody(string Text, JsonNode? Root, string? Note)
{
    /// <summary>Objects and arrays this many levels deep start open; deeper ones render only when opened.</summary>
    public const int OpenLevels = 2;

    public bool IsTree => Root is JsonObject or JsonArray;

    /// <summary>Reads a body; null when there is none.</summary>
    public static JsonBody? Read(string? text, bool truncated)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (truncated) return new(text, null, "This body was cut at 64 KB, so it is shown as text.");
        try
        {
            return new(text, JsonNode.Parse(text), null);
        }
        catch (JsonException)
        {
            return new(text, null, "This body is not JSON, so it is shown as text.");
        }
    }

    /// <summary>The body indented for reading; the text as it came when it isn't a tree.</summary>
    public string Pretty => IsTree ? Root!.ToJsonString(Indented) : Text;

    /// <summary>Paths (<see cref="JsonNode.GetPath"/>) of the objects and arrays that start open.</summary>
    public HashSet<string> OpenAtStart() => Containers(OpenLevels);

    /// <summary>Paths of every object and array, for Expand all.</summary>
    public HashSet<string> All() => Containers(int.MaxValue);

    /// <summary>What a folded object or array holds, e.g. "3 keys" or "100 items".</summary>
    public static string Summary(JsonNode node) => node switch
    {
        JsonObject o => o.Count == 1 ? "1 key" : $"{o.Count} keys",
        JsonArray a => a.Count == 1 ? "1 item" : $"{a.Count} items",
        _ => "",
    };

    /// <summary>The children of an object (labelled with their quoted name) or array (labelled with their index).</summary>
    public static IEnumerable<(string Label, JsonNode? Node)> Children(JsonNode node) => node switch
    {
        JsonObject o => o.Select(p => ($"\"{p.Key}\"", p.Value)),
        JsonArray a => a.Select((value, i) => (i.ToString(), value)),
        _ => [],
    };

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    private HashSet<string> Containers(int levels)
    {
        var paths = new HashSet<string>();
        if (IsTree) Walk(Root!, 0, levels, paths);
        return paths;
    }

    private static void Walk(JsonNode node, int depth, int levels, HashSet<string> paths)
    {
        if (depth >= levels || node is not (JsonObject or JsonArray)) return;
        paths.Add(node.GetPath());
        foreach (var (_, child) in Children(node))
            if (child is not null) Walk(child, depth + 1, levels, paths);
    }
}
