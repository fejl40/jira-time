using System.Net;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;

namespace JiraTime.Services;

/// <summary>
/// How every Jira request copes with Jira's rate limits, set up once on the <see cref="JiraHttp"/> client with
/// Microsoft.Extensions.Http.Resilience. There is one pipeline per Jira host, shared by every user and tab:
/// <list type="number">
/// <item>Retry (outermost): a 429 or the open circuit is waited out and the request sent again, a few times.</item>
/// <item>Concurrency limiter: at most <see cref="MaxInFlight"/> requests at once, the rest wait in a FIFO queue.</item>
/// <item>Circuit breaker: a 429 opens it for Retry-After plus jitter, so no request goes to that host meanwhile.</item>
/// </list>
/// The limiter sits outside the breaker, so a request turned away by the open circuit gives its place back
/// while the retry waits.
/// </summary>
public static class JiraResilience
{
    public const int MaxInFlight = 4;
    public const int MaxQueued = 200;
    public const int MaxRetries = 5;
    /// <summary>A longer pause than this isn't waited out: the request fails as it did before.</summary>
    public static readonly TimeSpan MaxRetryAfter = TimeSpan.FromSeconds(10);
    /// <summary>The pause is lengthened by up to this share, so waiting requests don't all come back at once.</summary>
    public const double MaxJitter = 0.2;

    public const string SlowDownMessage = "Jira asked us to slow down (429). Try again in a minute.";

    private static readonly ResiliencePropertyKey<TimeSpan?> RetryAfterKey = new("JiraTime.RetryAfter");

    /// <summary>Registers <see cref="JiraHttp"/> with its timeout, the resilience pipeline and the request log.</summary>
    public static IHttpClientBuilder AddJiraHttp(this IServiceCollection services)
    {
        var client = services.AddHttpClient<JiraHttp>(c => c.Timeout = TimeSpan.FromSeconds(30));
        client.AddResilienceHandler("jira", (pipeline, context) => Configure(pipeline, context.ServiceProvider.GetRequiredService<TimeProvider>()))
            .SelectPipelineByAuthority();
        // Inside the pipeline, so every attempt is logged.
        client.AddHttpMessageHandler(sp => new JiraRecordingHandler(sp.GetRequiredService<JiraRequestRecorder>(), sp.GetRequiredService<TimeProvider>()));
        return client;
    }

    public static void Configure(ResiliencePipelineBuilder<HttpResponseMessage> pipeline, TimeProvider clock)
    {
        pipeline.TimeProvider = clock;
        pipeline
            .AddRetry(Retry(clock))
            .AddConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = MaxInFlight, QueueLimit = MaxQueued, QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            })
            .AddCircuitBreaker(Breaker(clock));
    }

    private static HttpRetryStrategyOptions Retry(TimeProvider clock) => new()
    {
        MaxRetryAttempts = MaxRetries,
        BackoffType = DelayBackoffType.Exponential,
        Delay = TimeSpan.FromSeconds(1),
        MaxDelay = MaxRetryAfter,
        UseJitter = true,
        ShouldRetryAfterHeader = false, // the delay generator below reads Retry-After and the open circuit
        ShouldHandle = args => ValueTask.FromResult(ShouldRetry(args.Outcome, clock)),
        DelayGenerator = args => ValueTask.FromResult(DelayOf(args.Outcome, clock)),
    };

    /// <summary>
    /// Only a 429 or the open circuit: Jira did nothing with the request, so even a write is safe to send again.
    /// 5xx, timeouts and network errors are never retried, so a worklog is never logged twice.
    /// </summary>
    private static bool ShouldRetry(Outcome<HttpResponseMessage> outcome, TimeProvider clock) => outcome switch
    {
        { Exception: BrokenCircuitException ex } => (ex.RetryAfter ?? TimeSpan.Zero) <= MaxRetryAfter * (1 + MaxJitter),
        { Result.StatusCode: HttpStatusCode.TooManyRequests } => (RetryAfter(outcome.Result, clock) ?? TimeSpan.Zero) <= MaxRetryAfter,
        _ => false,
    };

    /// <summary>How long Jira asked to wait, or what is left of the pause; null leaves it to the exponential backoff.</summary>
    private static TimeSpan? DelayOf(Outcome<HttpResponseMessage> outcome, TimeProvider clock) => outcome switch
    {
        // A little past the end of the pause, so the retry never lands on its edge and finds it still closed.
        { Exception: BrokenCircuitException { RetryAfter: { } left } } => left + TimeSpan.FromMilliseconds(100),
        { Result: { } res } => RetryAfter(res, clock),
        _ => null,
    };

    private static CircuitBreakerStrategyOptions<HttpResponseMessage> Breaker(TimeProvider clock) => new()
    {
        // Any 429 opens it: the lowest failure ratio, over the fewest requests Polly allows in the window (two).
        FailureRatio = 0.0001,
        MinimumThroughput = 2,
        SamplingDuration = TimeSpan.FromMinutes(5),
        ShouldHandle = args =>
        {
            if (args.Outcome.Result?.StatusCode != HttpStatusCode.TooManyRequests) return ValueTask.FromResult(false);
            args.Context.Properties.Set(RetryAfterKey, RetryAfter(args.Outcome.Result, clock));
            return ValueTask.FromResult(true);
        },
        BreakDurationGenerator = args =>
            ValueTask.FromResult(WithJitter(args.Context.Properties.GetValue(RetryAfterKey, null) ?? TimeSpan.FromSeconds(1))),
    };

    private static TimeSpan WithJitter(TimeSpan pause) =>
        TimeSpan.FromMilliseconds(Math.Max(500, pause.TotalMilliseconds * (1 + Random.Shared.NextDouble() * MaxJitter)));

    /// <summary>The Retry-After header as a wait: seconds, or an HTTP date from now; null when Jira sent none.</summary>
    public static TimeSpan? RetryAfter(HttpResponseMessage res, TimeProvider clock) => res.Headers.RetryAfter switch
    {
        { Delta: { } delta } => delta,
        { Date: { } date } => date > clock.GetUtcNow() ? date - clock.GetUtcNow() : TimeSpan.Zero,
        _ => null,
    };

    /// <summary>What the request log and the toasts say about a 429.</summary>
    public static string WaitMessage(TimeSpan? retryAfter) => retryAfter is { } wait
        ? $"Jira asked us to slow down: requests to Jira pause for {Math.Max(1, Math.Ceiling(wait.TotalSeconds))} s."
        : "Jira asked us to slow down: requests to Jira pause for a moment.";
}
