namespace JiraTime.Domain.JiraLog;

/// <summary>One Jira request shown as a toast. <see cref="Leaving"/> is set while it fades out.</summary>
public sealed class RequestToast(JiraLogEntry entry)
{
    public JiraLogEntry Entry { get; internal set; } = entry;
    /// <summary>When it starts fading; null while the request is out or the toasts are paused.</summary>
    public DateTimeOffset? Until { get; internal set; }
    internal TimeSpan? Remaining { get; set; }
    public bool Leaving { get; internal set; }
}

/// <summary>
/// The toasts for live Jira requests: newest last, at most <see cref="MaxShown"/> shown with the rest
/// counted. A finished request stays <see cref="Show"/> (<see cref="ShowError"/> when it failed), then
/// fades for <see cref="Fade"/> and is gone. Hovering pauses the countdown.
/// </summary>
public sealed class RequestToasts(TimeProvider clock)
{
    public static readonly TimeSpan Show = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan ShowError = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan Fade = TimeSpan.FromMilliseconds(300);
    public const int MaxShown = 5;

    private readonly List<RequestToast> _toasts = [];
    private bool _paused;

    /// <summary>The newest toasts, oldest first, so the newest sits at the bottom.</summary>
    public IReadOnlyList<RequestToast> Shown => _toasts.Count <= MaxShown ? _toasts : _toasts[^MaxShown..];

    /// <summary>Toasts not shown because newer ones took their place: the "+N more" line.</summary>
    public int Hidden => Math.Max(0, _toasts.Count - MaxShown);

    public bool Any => _toasts.Count > 0;

    /// <summary>A request started or finished. A start that arrives after its finish is ignored.</summary>
    public void Update(JiraLogEntry entry)
    {
        var toast = _toasts.Find(t => t.Entry.Id == entry.Id);
        if (toast is null) _toasts.Add(toast = new RequestToast(entry));
        else if (entry.IsPending) return;
        toast.Entry = entry;
        if (!entry.IsPending && toast.Until is null && toast.Remaining is null) StartCountdown(toast, entry.Failed ? ShowError : Show);
    }

    /// <summary>Closes a toast at once, e.g. an error the user has read.</summary>
    public void Close(string id) => _toasts.RemoveAll(t => t.Entry.Id == id);

    /// <summary>The pointer is over the toasts: nothing fades until <see cref="Resume"/>.</summary>
    public void Pause()
    {
        if (_paused) return;
        _paused = true;
        var now = clock.GetUtcNow();
        foreach (var toast in _toasts.Where(t => t.Until is not null))
            (toast.Remaining, toast.Until, toast.Leaving) = (Max(toast.Until!.Value - now, Fade), null, false);
    }

    public void Resume()
    {
        _paused = false;
        foreach (var toast in _toasts.Where(t => t.Remaining is not null))
        {
            StartCountdown(toast, toast.Remaining!.Value);
            toast.Remaining = null;
        }
    }

    /// <summary>Moves toasts on to fading and removes faded ones. True when something changed.</summary>
    public bool Tick()
    {
        var now = clock.GetUtcNow();
        var changed = _toasts.RemoveAll(t => t.Until + Fade <= now) > 0;
        foreach (var toast in _toasts.Where(t => !t.Leaving && t.Until <= now))
            (toast.Leaving, changed) = (true, true);
        return changed;
    }

    private void StartCountdown(RequestToast toast, TimeSpan show)
    {
        if (_paused) toast.Remaining = show;
        else toast.Until = clock.GetUtcNow() + show;
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
}
