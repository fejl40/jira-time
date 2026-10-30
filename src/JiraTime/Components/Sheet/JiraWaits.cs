using JiraTime.Domain;

namespace JiraTime.Components;

/// <summary>What the timesheet waits on Jira for.</summary>
public enum JiraWait
{
    /// <summary>Reading the period's time (opening a period, reading it again after a save, Try again).</summary>
    Period,
    Sync,
    SaveAll,
}

/// <summary>
/// The timesheet's requests to Jira that are running, and what that means for the screen: which parts
/// show as pending, which buttons are off and what the status line says. One request at a time keeps
/// saves and reads from crossing each other.
/// </summary>
public sealed class JiraWaits
{
    private readonly Dictionary<JiraWait, int> _running = [];

    /// <summary>Raised when a request starts or ends, so parts that didn't start it show it too.</summary>
    public event Action? Changed;

    /// <summary>The period being opened while the old one stays on screen; null when none is.</summary>
    public Period? Opening { get; private set; }

    public bool Has(JiraWait wait) => _running.ContainsKey(wait);

    /// <summary>A button that sends a request to Jira works only while no other request runs.</summary>
    public bool CanStart => _running.Count == 0;

    /// <summary>The period's time is being read or changed: the grid and the day list are pending.</summary>
    public bool PeriodPending => !CanStart;

    /// <summary>Anything running may change the selected day's rows, so they are pending meanwhile.</summary>
    public bool DayPending => !CanStart;

    /// <summary>The period on screen is not the one asked for: another one is still being read.</summary>
    public bool IsStale(Period shown) => Opening is { } opening && opening != shown;

    /// <summary>What the period header says while waiting; null when nothing runs.</summary>
    public string? Status(Period shown)
    {
        if (Opening is { } opening && opening != shown) return $"Opening {Format.Range(opening.Start, opening.End)}…";
        if (Has(JiraWait.SaveAll)) return "Saving to Jira…";
        if (Has(JiraWait.Sync)) return "Syncing with Jira…";
        if (Has(JiraWait.Period)) return "Reading Jira…";
        return null;
    }

    /// <summary>Runs <paramref name="work"/> as a <paramref name="wait"/>, shown as running until it ends, also when it fails.</summary>
    public async Task RunAsync(JiraWait wait, Func<Task> work)
    {
        _running[wait] = _running.GetValueOrDefault(wait) + 1;
        Changed?.Invoke();
        try
        {
            await work();
        }
        finally
        {
            if (--_running[wait] == 0) _running.Remove(wait);
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Reads <paramref name="period"/> with <paramref name="read"/> while the old period stays on screen.
    /// Returns null when another period was asked for meanwhile, so this one isn't shown.
    /// </summary>
    public async Task<T?> OpenAsync<T>(Period period, Func<Task<T>> read) where T : class
    {
        Opening = _latest = period;
        T? result = null;
        try
        {
            await RunAsync(JiraWait.Period, async () => result = await read());
        }
        finally
        {
            if (Opening == period) Opening = null;
        }
        return _latest == period ? result : null;
    }

    /// <summary>The period asked for last; a period read before it was asked for is not shown.</summary>
    private Period? _latest;

    /// <summary>An aria-busy value: "true" or "false" (Blazor would write an empty value for a bool).</summary>
    public static string Aria(bool busy) => busy ? "true" : "false";
}
