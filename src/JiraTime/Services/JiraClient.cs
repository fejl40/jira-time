using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JiraTime.Domain;

namespace JiraTime.Services;

public class JiraException(string message, HttpStatusCode? status = null) : Exception(message)
{
    public HttpStatusCode? Status { get; } = status;
}

/// <summary>Jira REST calls. Runs on the server, so there are no CORS issues.</summary>
public class JiraClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>The user the token belongs to. Doubles as a token check.</summary>
    public async Task<JiraProfile> GetMyselfAsync(JiraConnection c, CancellationToken ct = default) =>
        ToProfile(await GetAsync<RawUser>(c, $"{Api(c)}/myself", ct));

    /// <summary>Issue search for autocomplete. Uses the issue picker, which exists on both Cloud and Server/DC.</summary>
    public async Task<IReadOnlyList<JiraIssue>> SearchIssuesAsync(JiraConnection c, string query, CancellationToken ct = default)
    {
        var result = await GetAsync<PickerResult>(c,
            $"/rest/api/2/issue/picker?query={Uri.EscapeDataString(query)}&showSubTasks=true", ct);
        return result.Sections
            .SelectMany(s => s.Issues)
            .DistinctBy(i => i.Key)
            .Select(i => new JiraIssue(i.Key, i.SummaryText ?? ""))
            .ToList();
    }

    /// <summary>
    /// Issues assigned to <paramref name="profile"/> in open sprints that are not Done (tasks, stories, ...).
    /// Falls back to all assigned, not-Done issues when the instance has no sprints (no Jira Software).
    /// </summary>
    public async Task<IReadOnlyList<SprintIssue>> GetSprintIssuesAsync(JiraConnection c, JiraProfile profile, CancellationToken ct = default)
    {
        var assignee = (profile.Username ?? profile.Id).Replace("\"", "\\\"");
        var mine = $"assignee = \"{assignee}\" AND statusCategory != Done";
        try
        {
            return await SearchAsync(c, $"{mine} AND sprint in openSprints() ORDER BY updated DESC", ct);
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.BadRequest)
        {
            return await SearchAsync(c, $"{mine} ORDER BY updated DESC", ct);
        }
    }

    private async Task<IReadOnlyList<SprintIssue>> SearchAsync(JiraConnection c, string jql, CancellationToken ct)
    {
        // Cloud retired GET /search in favour of /search/jql; Server/DC only has /search.
        var path = c.IsCloud ? "/rest/api/3/search/jql" : "/rest/api/2/search";
        var result = await GetAsync<SearchResult>(c,
            $"{path}?jql={Uri.EscapeDataString(jql)}&fields=summary,issuetype,status&maxResults=100", ct);
        return result.Issues
            .Select(i => new SprintIssue(i.Key, i.Fields?.Summary ?? "", i.Fields?.IssueType?.Name ?? "",
                i.Fields?.Status?.Name ?? "", i.Fields?.Status?.StatusCategory?.Key ?? ""))
            .ToList();
    }

    /// <summary>
    /// Worklogs <paramref name="profile"/> logged on days from <paramref name="from"/> to <paramref name="to"/>,
    /// on any issue, however they were logged.
    /// </summary>
    public async Task<IReadOnlyList<JiraWorklog>> GetMyWorklogsAsync(JiraConnection c, JiraProfile profile, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var author = (profile.Username ?? profile.Id).Replace("\"", "\\\"");
        var jql = $"worklogAuthor = \"{author}\" AND worklogDate >= \"{from:yyyy-MM-dd}\" AND worklogDate <= \"{to:yyyy-MM-dd}\"";
        var result = new List<JiraWorklog>();
        foreach (var issue in await SearchAllAsync(c, jql, ct))
        {
            for (var startAt = 0; ; )
            {
                var page = await GetAsync<WorklogPage>(c,
                    $"{Api(c)}/issue/{Uri.EscapeDataString(issue.Key)}/worklog?startAt={startAt}&maxResults=1000", ct);
                foreach (var w in page.Worklogs)
                {
                    if (w.Id is null || !IsAuthor(w.Author, profile) || ParseStarted(w.Started) is not { } date) continue;
                    if (date < from || date > to) continue;
                    result.Add(new JiraWorklog(w.Id, issue.Key, issue.Fields?.Summary ?? "", date,
                        WholeMinutes(w.TimeSpentSeconds), CommentText(w.Comment)));
                }
                startAt += page.Worklogs.Count;
                if (page.Worklogs.Count == 0 || startAt >= page.Total) break;
            }
        }
        return result;
    }

    /// <summary>One worklog, or null when Jira no longer has it.</summary>
    public async Task<JiraWorklog?> GetWorklogAsync(JiraConnection c, string issueKey, string worklogId, CancellationToken ct = default)
    {
        try
        {
            var w = await GetAsync<RawJiraWorklog>(c,
                $"{Api(c)}/issue/{Uri.EscapeDataString(issueKey)}/worklog/{Uri.EscapeDataString(worklogId)}", ct);
            return ParseStarted(w.Started) is { } date
                ? new JiraWorklog(w.Id ?? worklogId, issueKey, "", date, WholeMinutes(w.TimeSpentSeconds), CommentText(w.Comment))
                : null;
        }
        catch (JiraException ex) when (ex.Status == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>Worklog time in whole minutes (at least one), the unit entries are kept in.</summary>
    public static TimeSpan WholeMinutes(int seconds) => TimeSpan.FromMinutes(Math.Max(1, Math.Round(seconds / 60.0)));

    private static bool IsAuthor(RawUser? author, JiraProfile profile) =>
        author is not null && (author.AccountId == profile.Id || author.Key == profile.Id
                               || (profile.Username is not null && author.Name == profile.Username));

    /// <summary>The day of a worklog's "started" value, as Jira wrote it (2026-10-19T09:00:00.000+0200).</summary>
    public static DateOnly? ParseStarted(string? started) =>
        started is { Length: >= 10 } && DateOnly.TryParseExact(started[..10], "yyyy-MM-dd", out var d) ? d : null;

    /// <summary>Plain text of a worklog comment: a string on Server/DC, Atlassian Document Format on Cloud.</summary>
    public static string? CommentText(JsonElement? comment)
    {
        if (comment is not { } c) return null;
        string? text = c.ValueKind switch
        {
            JsonValueKind.String => c.GetString(),
            JsonValueKind.Object => string.Join("\n", Paragraphs(c)),
            _ => null,
        };
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();

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

    /// <summary>Every issue matching <paramref name="jql"/>, page by page.</summary>
    private async Task<List<SearchIssue>> SearchAllAsync(JiraConnection c, string jql, CancellationToken ct)
    {
        var all = new List<SearchIssue>();
        var path = c.IsCloud ? "/rest/api/3/search/jql" : "/rest/api/2/search";
        string? token = null;
        for (var page = 0; page < 50; page++)
        {
            var paging = c.IsCloud
                ? (token is null ? "" : $"&nextPageToken={Uri.EscapeDataString(token)}")
                : $"&startAt={all.Count}";
            var result = await GetAsync<SearchResult>(c, $"{path}?jql={Uri.EscapeDataString(jql)}&fields=summary&maxResults=100{paging}", ct);
            all.AddRange(result.Issues);
            token = result.NextPageToken;
            var more = c.IsCloud ? token is not null && result.IsLast != true : result.Total is { } total && all.Count < total;
            if (!more || result.Issues.Count == 0) break;
        }
        return all;
    }

    /// <summary>Creates a worklog for <paramref name="entry"/> on its issue.</summary>
    /// <returns>The new worklog id.</returns>
    public async Task<string> AddWorklogAsync(JiraConnection c, TimeEntry entry, CancellationToken ct = default)
    {
        var created = await SendAsync<RawWorklog>(c, HttpMethod.Post,
            $"{Api(c)}/issue/{Uri.EscapeDataString(entry.IssueKey)}/worklog?adjustEstimate=auto", WorklogBody(c, entry), ct);
        return created?.Id ?? throw new JiraException("Jira did not return a worklog id.");
    }

    /// <summary>Overwrites an existing worklog on <paramref name="issueKey"/> with the entry's time and comment.</summary>
    public Task UpdateWorklogAsync(JiraConnection c, string issueKey, string worklogId, TimeEntry entry, CancellationToken ct = default) =>
        SendAsync<RawWorklog>(c, HttpMethod.Put,
            $"{Api(c)}/issue/{Uri.EscapeDataString(issueKey)}/worklog/{Uri.EscapeDataString(worklogId)}?adjustEstimate=auto", WorklogBody(c, entry), ct);

    public Task DeleteWorklogAsync(JiraConnection c, string issueKey, string worklogId, CancellationToken ct = default) =>
        SendAsync<object>(c, HttpMethod.Delete,
            $"{Api(c)}/issue/{Uri.EscapeDataString(issueKey)}/worklog/{Uri.EscapeDataString(worklogId)}?adjustEstimate=auto", null, ct);

    /// <summary>Jira's worklog "started" format, e.g. 2026-10-19T09:00:00.000+0200 (offset without a colon).</summary>
    public static string FormatStarted(DateOnly date, TimeOnly time, TimeZoneInfo? zone = null)
    {
        var local = date.ToDateTime(time);
        var offset = (zone ?? TimeZoneInfo.Local).GetUtcOffset(local);
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        return $"{local:yyyy-MM-dd'T'HH:mm:ss}.000{sign}{Math.Abs(offset.Hours):00}{Math.Abs(offset.Minutes):00}";
    }

    private static object WorklogBody(JiraConnection c, TimeEntry e)
    {
        var started = FormatStarted(e.Date, TimeEntry.JiraStartTime);
        var seconds = (int)e.Duration.TotalSeconds;
        var text = e.Description ?? "";
        if (!c.IsCloud)
            return new { started, timeSpentSeconds = seconds, comment = text };

        // Cloud (API v3) wants comments as Atlassian Document Format.
        object comment = new
        {
            type = "doc",
            version = 1,
            content = string.IsNullOrEmpty(text)
                ? Array.Empty<object>()
                : [new { type = "paragraph", content = new[] { new { type = "text", text } } }],
        };
        return new { started, timeSpentSeconds = seconds, comment };
    }

    // Cloud has v3; Server/DC only v2.
    private static string Api(JiraConnection c) => c.IsCloud ? "/rest/api/3" : "/rest/api/2";

    private Task<T> GetAsync<T>(JiraConnection c, string path, CancellationToken ct) =>
        SendAsync<T>(c, HttpMethod.Get, path, null, ct)!;

    private async Task<T> SendAsync<T>(JiraConnection c, HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, c.BaseUrl.TrimEnd('/') + path);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (body is not null)
            req.Content = JsonContent.Create(body, options: Json);
        req.Headers.Authorization = c.IsCloud
            ? new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{c.Email}:{c.Token}")))
            : new AuthenticationHeaderValue("Bearer", c.Token);

        HttpResponseMessage res;
        try
        {
            res = await http.SendAsync(req, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new JiraException($"Could not reach {c.Host}: {ex.InnerException?.Message ?? ex.Message}");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new JiraException($"{c.Host} did not respond in time.");
        }

        using (res)
        {
            if (res.StatusCode == HttpStatusCode.Unauthorized)
                throw new JiraException("Jira rejected the token (401). Check that it is correct and not expired.", res.StatusCode);

            // Nothing to read (e.g. 204 after a delete).
            if (res.IsSuccessStatusCode && (res.StatusCode == HttpStatusCode.NoContent || method == HttpMethod.Delete))
                return default!;

            // Jira sometimes answers a bad token with a login page instead of a 401.
            var isJson = res.Content.Headers.ContentType?.MediaType?.Contains("json") == true;
            if (res.IsSuccessStatusCode && !isJson)
                throw new JiraException($"{c.Host} did not return JSON. Is the URL the Jira base URL?", res.StatusCode);

            if (!res.IsSuccessStatusCode)
            {
                var message = $"Jira returned {(int)res.StatusCode} {res.ReasonPhrase}.";
                if (isJson)
                {
                    try
                    {
                        var err = await res.Content.ReadFromJsonAsync<JiraErrorBody>(Json, ct);
                        var msgs = (err?.ErrorMessages ?? []).Concat(err?.Errors?.Values.AsEnumerable() ?? []).ToList();
                        if (msgs.Count > 0) message = string.Join(" ", msgs);
                    }
                    catch (JsonException) { }
                }
                throw new JiraException(message, res.StatusCode);
            }

            return await res.Content.ReadFromJsonAsync<T>(Json, ct)
                   ?? throw new JiraException("Jira returned an empty response.");
        }
    }

    private static JiraProfile ToProfile(RawUser u) =>
        new(u.AccountId ?? u.Key ?? u.Name ?? "", u.Name, u.DisplayName ?? u.Name ?? "?", u.EmailAddress);

    private record RawUser(string? AccountId, string? Key, string? Name, string? DisplayName, string? EmailAddress);
    private record PickerResult(List<PickerSection> Sections);
    private record PickerSection(List<PickerIssue> Issues);
    private record PickerIssue(string Key, string? SummaryText);
    private record RawWorklog(string? Id);
    private record SearchResult(List<SearchIssue> Issues, int? Total = null, string? NextPageToken = null, bool? IsLast = null);
    private record WorklogPage(List<RawJiraWorklog> Worklogs, int Total);
    private record RawJiraWorklog(string? Id, string? Started, int TimeSpentSeconds, JsonElement? Comment, RawUser? Author);
    private record SearchIssue(string Key, SearchFields? Fields);
    private record SearchFields(string? Summary, NamedField? IssueType, StatusField? Status);
    private record NamedField(string? Name);
    private record StatusField(string? Name, StatusCategoryField? StatusCategory);
    private record StatusCategoryField(string? Key);
    private record JiraErrorBody(List<string>? ErrorMessages, Dictionary<string, string>? Errors);
}
