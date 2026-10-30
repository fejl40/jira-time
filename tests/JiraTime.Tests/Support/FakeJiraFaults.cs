using System.Net;

namespace JiraTime.Tests.Support;

/// <summary>The knobs of <see cref="FakeJira"/> that make Jira fail, refuse with 429, go offline or hold requests back.</summary>
public sealed partial class FakeJira
{
    /// <summary>When set, every request fails with this status.</summary>
    public HttpStatusCode? FailWith { get; set; }

    /// <summary>When set and it returns a status, that request fails with it.</summary>
    public Func<Uri, HttpStatusCode?>? FailWhen { get; set; }

    /// <summary>How many of the next requests Jira refuses with 429 Too Many Requests.</summary>
    public int TooManyRequests { get; set; }

    /// <summary>The Retry-After header sent with a 429 (seconds or an HTTP date); none when null.</summary>
    public string? RetryAfter { get; set; }

    /// <summary>When set, requests throw as if the server can't be reached.</summary>
    public bool Offline { get; set; }

    /// <summary>When set, requests wait for it before Jira answers, so tests can see how many are out at once.</summary>
    public TaskCompletionSource? Hold { get; set; }

    /// <summary>The requests <see cref="Hold"/> holds back; all of them when null.</summary>
    public Func<Request, bool>? HoldWhen { get; set; }

    /// <summary>Released once for each request that reached Jira.</summary>
    public SemaphoreSlim Arrived { get; } = new(0);

    /// <summary>The most requests that were out at the same time.</summary>
    public int MostInFlight { get; private set; }

    private int _inFlight;

    /// <summary>Notes a request that just arrived; called under the lock.</summary>
    private void Arrive() => MostInFlight = Math.Max(MostInFlight, ++_inFlight);

    /// <summary>Notes a request that was answered; called under the lock.</summary>
    private void Leave() => _inFlight--;

    /// <summary>Holds the request back while <see cref="Hold"/> says so.</summary>
    private Task HoldAsync(Request request, CancellationToken ct) =>
        Hold is { } hold && HoldWhen?.Invoke(request) != false ? hold.Task.WaitAsync(ct) : Task.CompletedTask;

    /// <summary>A 429 or the failure a test asked for; null when Jira answers as usual.</summary>
    private HttpResponseMessage? Refusal(Uri uri)
    {
        if (TooManyRequests > 0)
        {
            TooManyRequests--;
            var refused = Json(HttpStatusCode.TooManyRequests, new { errorMessages = new[] { "Rate limit exceeded" } });
            if (RetryAfter is { } after) refused.Headers.TryAddWithoutValidation("Retry-After", after);
            return refused;
        }

        if ((FailWith ?? FailWhen?.Invoke(uri)) is { } status)
            return Json(status, new { errorMessages = new[] { $"Failed with {(int)status}" } });
        return null;
    }
}
