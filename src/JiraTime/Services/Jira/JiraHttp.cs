using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JiraTime.Domain;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using JiraTime.Services.JiraLog;

namespace JiraTime.Services.Jira;

/// <summary>A Jira request that failed, with a message the user can act on and Jira's status when it answered.</summary>
public class JiraException(string message, HttpStatusCode? status = null) : Exception(message)
{
    public HttpStatusCode? Status { get; } = status;
}

/// <summary>
/// The one typed HttpClient for Jira: picks the API version, adds auth, and turns failures into <see cref="JiraException"/>s
/// with messages a user can act on. Retries, the rate-limit pause and the queue are its resilience pipeline
/// (<see cref="JiraResilience"/>); each attempt is logged by <see cref="JiraRecordingHandler"/>.
/// </summary>
public class JiraHttp(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>The REST API root: Cloud has v3, Server/DC only v2.</summary>
    public static string Api(JiraConnection c) => c.IsCloud ? "/rest/api/3" : "/rest/api/2";

    /// <summary>The JQL search endpoint. Cloud retired GET /search in favour of /search/jql; Server/DC only has /search.</summary>
    public static string SearchPath(JiraConnection c) => c.IsCloud ? "/rest/api/3/search/jql" : "/rest/api/2/search";

    public Task<T> GetAsync<T>(JiraConnection c, string path, CancellationToken ct) =>
        SendAsync<T>(c, HttpMethod.Get, path, null, ct)!;

    /// <summary>Sends <paramref name="body"/> as JSON and reads the answer as <typeparamref name="T"/> (default when there is none).</summary>
    public async Task<T> SendAsync<T>(JiraConnection c, HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var json = body is null ? null : JsonSerializer.Serialize(body, Json);
        using var req = new HttpRequestMessage(method, c.BaseUrl.TrimEnd('/') + path);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (json is not null)
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        req.Headers.Authorization = c.IsCloud
            ? new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{c.Email}:{c.Token}")))
            : new AuthenticationHeaderValue("Bearer", c.Token);
        var request = JiraRequest.Attach(req, c, json);

        // Every attempt that reached Jira is logged; the last one is finished here, with how the request ended.
        try
        {
            var result = await SendAsync<T>(c, req, ct);
            await request.FinishAsync(null);
            return result;
        }
        catch (Exception ex)
        {
            await request.FinishAsync(ex is OperationCanceledException ? "Cancelled before Jira answered." : ex.Message);
            throw;
        }
    }

    private async Task<T> SendAsync<T>(JiraConnection c, HttpRequestMessage req, CancellationToken ct)
    {
        var method = req.Method;
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
        catch (BrokenCircuitException)
        {
            throw new JiraException(JiraResilience.SlowDownMessage, HttpStatusCode.TooManyRequests);
        }
        catch (RateLimiterRejectedException)
        {
            throw new JiraException("Too many requests are waiting for Jira. Try again in a moment.", HttpStatusCode.TooManyRequests);
        }

        using (res)
        {
            if (res.StatusCode == HttpStatusCode.Unauthorized)
                throw new JiraException("Jira rejected the token (401). Check that it is correct and not expired.", res.StatusCode);
            if (res.StatusCode == HttpStatusCode.TooManyRequests)
                throw new JiraException(JiraResilience.SlowDownMessage, res.StatusCode);

            // Nothing to read (e.g. 204 after a delete).
            if (res.IsSuccessStatusCode && (res.StatusCode == HttpStatusCode.NoContent || method == HttpMethod.Delete))
                return default!;

            // Jira sometimes answers a bad token with a login page instead of a 401.
            var isJson = res.Content.Headers.ContentType?.MediaType?.Contains("json") == true;
            if (res.IsSuccessStatusCode && !isJson)
                throw new JiraException($"{c.Host} did not return JSON. Is the URL the Jira base URL?", res.StatusCode);

            if (!res.IsSuccessStatusCode)
                throw new JiraException(await ErrorMessageAsync(res, isJson, ct), res.StatusCode);

            return await res.Content.ReadFromJsonAsync<T>(Json, ct)
                   ?? throw new JiraException("Jira returned an empty response.");
        }
    }

    /// <summary>Jira's own error messages when it sent any, else the status.</summary>
    private static async Task<string> ErrorMessageAsync(HttpResponseMessage res, bool isJson, CancellationToken ct)
    {
        var message = $"Jira returned {(int)res.StatusCode} {res.ReasonPhrase}.";
        if (!isJson) return message;
        try
        {
            var err = await res.Content.ReadFromJsonAsync<JiraErrorBody>(Json, ct);
            var msgs = (err?.ErrorMessages ?? []).Concat(err?.Errors?.Values.AsEnumerable() ?? []).ToList();
            if (msgs.Count > 0) message = string.Join(" ", msgs);
        }
        catch (JsonException) { }
        return message;
    }
}
