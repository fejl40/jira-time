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

    public async Task<IReadOnlyList<JiraProfile>> SearchUsersAsync(JiraConnection c, string query, CancellationToken ct = default)
    {
        var q = Uri.EscapeDataString(query);
        // Server/DC calls the parameter `username`, but it matches name, display name and email.
        var path = c.IsCloud
            ? $"{Api(c)}/user/search?query={q}&maxResults=20"
            : $"{Api(c)}/user/search?username={q}&maxResults=20";
        var users = await GetAsync<List<RawUser>>(c, path, ct);
        return users.Where(u => u.Active != false).Select(ToProfile).ToList();
    }

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

    // Cloud has v3; Server/DC only v2.
    private static string Api(JiraConnection c) => c.IsCloud ? "/rest/api/3" : "/rest/api/2";

    private async Task<T> GetAsync<T>(JiraConnection c, string path, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, c.BaseUrl.TrimEnd('/') + path);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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

    private record RawUser(string? AccountId, string? Key, string? Name, string? DisplayName, string? EmailAddress, bool? Active);
    private record PickerResult(List<PickerSection> Sections);
    private record PickerSection(List<PickerIssue> Issues);
    private record PickerIssue(string Key, string? SummaryText);
    private record JiraErrorBody(List<string>? ErrorMessages, Dictionary<string, string>? Errors);
}
