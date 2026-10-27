using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JiraTime.Domain;

namespace JiraTime.Services;

public class JiraException(string message, HttpStatusCode? status = null) : Exception(message)
{
    public HttpStatusCode? Status { get; } = status;
}

/// <summary>
/// Sends requests to Jira: picks the API version, adds auth, and turns failures into <see cref="JiraException"/>s
/// with messages a user can act on.
/// </summary>
internal class JiraHttp(HttpClient http)
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
}
