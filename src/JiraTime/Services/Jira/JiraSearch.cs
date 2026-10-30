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

    /// <summary>
    /// Every issue matching <paramref name="jql"/> with <paramref name="fields"/> (comma separated). On Server/DC the first
    /// page gives the total, so the other pages are asked for together; Cloud's pages follow each other's page token.
    /// </summary>
    /// <param name="skipUnknownKeys">Asks Jira to warn about keys it doesn't know instead of refusing the whole search.</param>
    public static async Task<List<T>> SearchAllAsync<T>(this JiraHttp jira, JiraConnection c, string jql, string fields, CancellationToken ct,
        bool skipUnknownKeys = false)
    {
        var first = new SearchQuery(jql, fields, 0, null, skipUnknownKeys);
        var page = await SearchPageAsync<T>(jira, c, first, ct);
        var all = new List<T>(page.Issues);
        if (!c.IsCloud && await OtherPagesTogetherAsync(jira, c, first, page, ct) is { } rest)
        {
            all.AddRange(rest);
            return all;
        }
        for (var n = 1; n < MaxPages && HasMore(c, page, all.Count) && page.Issues.Count > 0; n++)
        {
            page = await SearchPageAsync<T>(jira, c, first with { StartAt = all.Count, PageToken = page.NextPageToken }, ct);
            all.AddRange(page.Issues);
        }
        return all;
    }

    private static bool HasMore<T>(JiraConnection c, SearchPage<T> page, int read) =>
        c.IsCloud ? page.NextPageToken is not null && page.IsLast != true : page.Total is { } total && read < total;

    /// <summary>
    /// The rest of a Server/DC search after its first page, every page asked for at once and joined in order; empty when
    /// the first page had it all. Null when the pages don't fit together (the answer changed meanwhile, or Jira sent a
    /// page shorter than the first): then the rest is read page by page, as it always was.
    /// </summary>
    private static async Task<List<T>?> OtherPagesTogetherAsync<T>(JiraHttp jira, JiraConnection c, SearchQuery first, SearchPage<T> page,
        CancellationToken ct)
    {
        var step = page.Issues.Count;
        if (page.Total is not { } total || step == 0 || step >= total) return [];
        var starts = Enumerable.Range(1, MaxPages - 1).Select(n => n * step).TakeWhile(start => start < total).ToList();
        var pages = await Task.WhenAll(starts.Select(start => SearchPageAsync<T>(jira, c, first with { StartAt = start }, ct)));
        var fits = pages.Select((p, i) => p.Total == total && p.Issues.Count == Math.Min(step, total - starts[i])).All(ok => ok);
        return fits ? pages.SelectMany(p => p.Issues).ToList() : null;
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
            // The keys are asked together; the client's limiter decides how many are out at once.
            var each = await Task.WhenAll(keys.Select(key => BatchAsync<T>(jira, c, [key], fields, ct)));
            return each.SelectMany(issues => issues).ToList();
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
