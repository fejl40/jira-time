using System.Net;
using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>
/// What the handlers below <see cref="JiraHttp"/> need to know about a request: whose connection it is, the JSON
/// sent, and the attempt being logged. Travels in the request's options, through every retry.
/// </summary>
public sealed class JiraRequest
{
    private static readonly HttpRequestOptionsKey<JiraRequest> Key = new("JiraTime.JiraRequest");

    public required JiraConnection Connection { get; init; }
    public string? Body { get; init; }
    /// <summary>The latest attempt that reached Jira; null until one did.</summary>
    public JiraCall? Call { get; set; }

    public static JiraRequest Attach(HttpRequestMessage req, JiraConnection c, string? body)
    {
        var request = new JiraRequest { Connection = c, Body = body };
        req.Options.Set(Key, request);
        return request;
    }

    public static JiraRequest? Of(HttpRequestMessage req) => req.Options.TryGetValue(Key, out var r) ? r : null;

    /// <summary>Logs the latest attempt with how the request ended (an attempt already logged stays as it was).</summary>
    public Task FinishAsync(string? error) => Call?.FinishAsync(error) ?? Task.CompletedTask;
}

/// <summary>
/// Logs each attempt of a Jira request, retries included: it sits inside the resilience pipeline, next to the
/// network. A 429 is logged here at once, saying how long Jira asked to wait; any other attempt is finished
/// by <see cref="JiraHttp"/>, which knows how the request ended.
/// </summary>
public sealed class JiraRecordingHandler(JiraRequestRecorder recorder, TimeProvider clock) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        if (JiraRequest.Of(req) is not { } request) return await base.SendAsync(req, ct);

        var call = request.Call = recorder.Start(request.Connection, req, request.Body);
        var res = await base.SendAsync(req, ct);
        await call.ReadResponseAsync(res, ct);
        if (res.StatusCode == HttpStatusCode.TooManyRequests)
            await call.FinishAsync(JiraResilience.WaitMessage(JiraResilience.RetryAfter(res, clock)));
        return res;
    }
}
