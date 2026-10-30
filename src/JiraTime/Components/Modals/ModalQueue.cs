namespace JiraTime.Components;

/// <summary>
/// One modal asked for: the component to show with its parameters, how it may be closed, and the result
/// its caller waits for. A custom modal gets it as a cascading parameter, to close itself with a result,
/// cancel, or show that it is busy.
/// </summary>
public sealed class ModalContext(int id, Type component, IDictionary<string, object?> parameters)
{
    private readonly TaskCompletionSource<object?> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _busy;

    public int Id { get; } = id;
    public Type Component { get; } = component;
    public IDictionary<string, object?> Parameters { get; } = parameters;

    /// <summary>Esc and a click on the backdrop cancel the modal; false for a modal that must be answered.</summary>
    public bool Dismiss { get; init; } = true;

    /// <summary>The modal waits for something (a request to Jira): its buttons are off and it can't be closed.</summary>
    public bool Busy
    {
        get => _busy;
        set
        {
            if (_busy == value) return;
            _busy = value;
            Owner?.Refresh();
        }
    }

    /// <summary>Esc and the backdrop close it now.</summary>
    public bool CanDismiss => Dismiss && !Busy;

    /// <summary>The value the modal closed with; null when it was cancelled.</summary>
    public Task<object?> Result => _result.Task;

    /// <summary>The service that shows it; null for a modal that isn't shown (in tests).</summary>
    internal Modals? Owner { get; set; }

    /// <summary>Closes the modal with a result for its caller.</summary>
    public void Close(object? result) => Owner?.Close(this, result);

    /// <summary>Closes the modal with no result.</summary>
    public void Cancel() => Close(null);

    /// <summary>Runs <paramref name="work"/> with the modal busy, also when it fails.</summary>
    public async Task WhileBusyAsync(Func<Task> work)
    {
        Busy = true;
        try
        {
            await work();
        }
        finally
        {
            Busy = false;
        }
    }

    internal void Complete(object? result) => _result.TrySetResult(result);
}

/// <summary>
/// Which modals are open and which wait their turn. One modal shows at a time: one asked for while
/// another is open waits, and they open in the order asked for. A modal opened on top (from inside a
/// modal) shows above the open one, which comes back when it closes.
/// </summary>
public sealed class ModalQueue
{
    private readonly List<ModalContext> _open = [];
    private readonly List<ModalContext> _waiting = [];

    /// <summary>The open modals, the lowest first; the last one is on top.</summary>
    public IReadOnlyList<ModalContext> Open => _open;

    /// <summary>Modals waiting for the open ones to close, the next first.</summary>
    public IReadOnlyList<ModalContext> Waiting => _waiting;

    public void Add(ModalContext modal, bool onTop = false)
    {
        if (_open.Count == 0 || onTop) _open.Add(modal);
        else _waiting.Add(modal);
    }

    /// <summary>
    /// Closes an open or waiting modal with <paramref name="result"/>; the next waiting modal opens once
    /// none is open. False when the modal was closed already.
    /// </summary>
    public bool Close(ModalContext modal, object? result)
    {
        if (!_open.Remove(modal) && !_waiting.Remove(modal)) return false;
        modal.Complete(result);
        if (_open.Count == 0 && _waiting.Count > 0)
        {
            _open.Add(_waiting[0]);
            _waiting.RemoveAt(0);
        }
        return true;
    }

    /// <summary>Cancels every open and waiting modal, e.g. when the user goes to another page.</summary>
    public void CloseAll()
    {
        var all = _open.Concat(_waiting).ToList();
        _open.Clear();
        _waiting.Clear();
        foreach (var modal in all) modal.Complete(null);
    }
}
