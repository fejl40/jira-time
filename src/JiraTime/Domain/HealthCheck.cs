namespace JiraTime.Domain;

/// <summary>One thing the health check asks Jira, in the order they run.</summary>
public enum HealthCheck
{
    /// <summary>Jira accepts the token and says whose it is.</summary>
    Token,
    /// <summary>Jira's worklog search finds the user's worklogs, as the timesheet reads them.</summary>
    Worklogs,
    /// <summary>The issue picker's sprint search and Jira search answer.</summary>
    IssuePicker,
    /// <summary>The user may log work on their issues; nothing is written.</summary>
    LoggingTime,
    /// <summary>Worklogs started at 09:00 in the browser's time zone land on the same day in Jira.</summary>
    TimeZone,
    /// <summary>This server's clock agrees with Jira's, so today is the right day.</summary>
    Clock,
}

/// <summary>Where a check is: waiting for its turn, running, or finished one of three ways.</summary>
public enum HealthState { Waiting, Running, Passed, Failed, Skipped }

/// <summary>
/// A check's outcome: <see cref="Detail"/> says what was found when it passed, how to fix it when it failed, and why it
/// didn't run when it was skipped.
/// </summary>
public sealed record HealthResult(HealthCheck Check, HealthState State, string? Detail = null)
{
    public static HealthResult Passed(HealthCheck check, string detail) => new(check, HealthState.Passed, detail);
    public static HealthResult Failed(HealthCheck check, string fix) => new(check, HealthState.Failed, fix);

    public bool IsFinished => State is HealthState.Passed or HealthState.Failed or HealthState.Skipped;
}

/// <summary>
/// One run of the health check: every check's state, in order. A failed token check skips every check after it,
/// since they all need Jira to accept the token.
/// </summary>
public sealed class HealthReport
{
    /// <summary>The checks in the order they run.</summary>
    public static readonly IReadOnlyList<HealthCheck> Order = Enum.GetValues<HealthCheck>();

    private readonly Dictionary<HealthCheck, HealthResult> _results = Order.ToDictionary(c => c, c => new HealthResult(c, HealthState.Waiting));

    /// <summary>Every check's latest result, in order.</summary>
    public IReadOnlyList<HealthResult> Results => Order.Select(c => _results[c]).ToList();

    public HealthResult this[HealthCheck check] => _results[check];

    /// <summary>True when every check has finished.</summary>
    public bool IsDone => _results.Values.All(r => r.IsFinished);

    public int Count(HealthState state) => _results.Values.Count(r => r.State == state);

    /// <summary>Share of the checks that finished, 0 to 1, for a progress bar.</summary>
    public double Progress => (double)_results.Values.Count(r => r.IsFinished) / Order.Count;

    /// <summary>Whether <paramref name="check"/> still has to run: it was not skipped or finished already.</summary>
    public bool ShouldRun(HealthCheck check) => !_results[check].IsFinished;

    /// <summary>Marks <paramref name="check"/> as running.</summary>
    public void Start(HealthCheck check) => _results[check] = new HealthResult(check, HealthState.Running);

    /// <summary>Records a finished check; a failed token check skips the rest.</summary>
    public void Finish(HealthResult result)
    {
        _results[result.Check] = result;
        if (result.Check != HealthCheck.Token || result.State != HealthState.Failed) return;
        foreach (var later in Order.Where(c => c != HealthCheck.Token))
            _results[later] = new HealthResult(later, HealthState.Skipped, "Skipped: it needs Jira to accept the token first.");
    }

    /// <summary>One sentence about the whole run, once it is done.</summary>
    public string Summary()
    {
        var failed = Count(HealthState.Failed);
        if (failed == 0) return $"All {Order.Count} checks passed. Jira Time has everything it needs.";
        var skipped = Count(HealthState.Skipped);
        var needs = failed == 1 ? "1 check needs attention." : $"{failed} checks need attention.";
        return skipped == 0 ? needs : $"{needs} {skipped} more couldn't run.";
    }

    /// <summary>The check's name as the user reads it.</summary>
    public static string Title(HealthCheck check) => check switch
    {
        HealthCheck.Token => "Token",
        HealthCheck.Worklogs => "Reading your worklogs",
        HealthCheck.IssuePicker => "Issue picker",
        HealthCheck.LoggingTime => "Logging time",
        HealthCheck.TimeZone => "Time zone",
        HealthCheck.Clock => "Clock",
        _ => check.ToString(),
    };

    /// <summary>What the check asks, shown while it waits or runs.</summary>
    public static string Question(HealthCheck check) => check switch
    {
        HealthCheck.Token => "Does Jira accept the token, and whose is it?",
        HealthCheck.Worklogs => "Can Jira's worklog search find your worklogs of the last 7 days?",
        HealthCheck.IssuePicker => "Do the sprint search and Jira search answer?",
        HealthCheck.LoggingTime => "May you log work on your issues? Nothing is written.",
        HealthCheck.TimeZone => "Do worklogs land on the right day in Jira?",
        HealthCheck.Clock => "Does this server's clock agree with Jira's?",
        _ => "",
    };
}
