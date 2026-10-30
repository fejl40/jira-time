using System.Net;
using JiraTime.Domain;
using static JiraTime.Services.Jira.JiraHttp;

namespace JiraTime.Services.Jira;

/// <summary>
/// Jira's issue search, the one way the app asks for several issues at once: it reads every page of the answer,
/// sends a search too long for a URL in a POST body, and reads issues by key in batches of <see cref="PageSize"/>.
/// </summary>
internal static class JiraSearch
{
    /// <summary>Issues asked for per page, and keys per <c>key in (...)</c> search.</summary>
    public const int PageSize = 100;

    /// <summary>A search whose URL would be longer than this goes in a POST body, so it stays within what Jira and the proxies in front of it take.</summary>
    public const int LongestUrl = 2000;

    private const int MaxPages = 50;

    /// <summary>Every issue matching <paramref name="jql"/> with <paramref name="fields"/> (comma separated), page by page.</summary>
    /// <param name="skipUnknownKeys">Asks Jira to warn about keys it doesn't know instead of refusing the whole search.</param>
    public static async Task<List<T>> SearchAllAsync<T>(this JiraHttp jira, JiraConnection c, string jql, string fields, CancellationToken ct,
        bool skipUnknownKeys = false)
    {
        var all = new List<T>();
        string? token = null;
        for (var page = 0; page < MaxPages; page++)
        {
            var query = new SearchQuery(jql, fields, all.Count, token, skipUnknownKeys);
            var result = await SearchPageAsync<T>(jira, c, query, ct);
            all.AddRange(result.Issues);
            token = result.NextPageToken;
            var more = c.IsCloud ? token is not null && result.IsLast != true : result.Total is { } total && all.Count < total;
            if (!more || result.Issues.Count == 0) break;
        }
        return all;
    }

    /// <summary>
    /// The issues with the given keys, <see cref="PageSize"/> keys per search; the batches go out together. Keys
    /// Jira doesn't know are left out.
    /// </summary>
    public static async Task<List<T>> ByKeysAsync<T>(this JiraHttp jira, JiraConnection c, IEnumerable<string> keys, string fields, CancellationToken ct)
    {
        var batches = keys.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(PageSize)
            .Select(batch => BatchAsync<T>(jira, c, batch, fields, ct));
        var found = await Task.WhenAll(batches);
        return found.SelectMany(issues => issues).ToList();
    }

    /// <summary>
    /// One batch of keys. Server/DC is asked to skip unknown keys (validateQuery=warn); a Jira that still refuses
    /// the search over one unknown key (Cloud, older Jiras) is asked each key on its own, and an unknown key gives nothing.
    /// </summary>
    private static async Task<List<T>> BatchAsync<T>(JiraHttp jira, JiraConnection c, IReadOnlyCollection<string> keys, string fields, CancellationToken ct)
    {
        try
        {
            return await jira.SearchAllAsync<T>(c, $"key in {Jql.List(keys)}", fields, ct, skipUnknownKeys: true);
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.BadRequest && keys.Count > 1)
        {
            var found = new List<T>();
            foreach (var key in keys)
                found.AddRange(await BatchAsync<T>(jira, c, [key], fields, ct));
            return found;
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.BadRequest)
        {
            return [];
        }
    }

    /// <summary>One page of a search: where it starts (startAt on Server/DC, the page token on Cloud) and what it asks for.</summary>
    private sealed record SearchQuery(string Jql, string Fields, int StartAt, string? PageToken, bool SkipUnknownKeys);

    private static Task<SearchPage<T>> SearchPageAsync<T>(JiraHttp jira, JiraConnection c, SearchQuery q, CancellationToken ct)
    {
        // Cloud's search has no validateQuery; only Server/DC is asked to warn.
        var warn = q.SkipUnknownKeys && !c.IsCloud;
        var paging = c.IsCloud
            ? (q.PageToken is null ? "" : $"&nextPageToken={Uri.EscapeDataString(q.PageToken)}")
            : $"&startAt={q.StartAt}";
        var validate = warn ? "&validateQuery=warn" : "";
        var url = $"{SearchPath(c)}?jql={Uri.EscapeDataString(q.Jql)}&fields={q.Fields}&maxResults={PageSize}{paging}{validate}";
        return url.Length <= LongestUrl
            ? jira.GetAsync<SearchPage<T>>(c, url, ct)
            : jira.SendAsync<SearchPage<T>>(c, HttpMethod.Post, SearchPath(c), PostBody(c, q, warn), ct);
    }

    private static Dictionary<string, object> PostBody(JiraConnection c, SearchQuery q, bool warn)
    {
        var body = new Dictionary<string, object> { ["jql"] = q.Jql, ["fields"] = q.Fields.Split(','), ["maxResults"] = PageSize };
        if (c.IsCloud && q.PageToken is not null) body["nextPageToken"] = q.PageToken;
        if (!c.IsCloud) body["startAt"] = q.StartAt;
        if (warn) body["validateQuery"] = "warn";
        return body;
    }
}
