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
    private record SearchResult(List<SearchIssue> Issues);
    private record SearchIssue(string Key, SearchFields? Fields);
    private record SearchFields(string? Summary, NamedField? IssueType, StatusField? Status);
    private record NamedField(string? Name);
    private record StatusField(string? Name, StatusCategoryField? StatusCategory);
    private record StatusCategoryField(string? Key);
    private record JiraErrorBody(List<string>? ErrorMessages, Dictionary<string, string>? Errors);
}
