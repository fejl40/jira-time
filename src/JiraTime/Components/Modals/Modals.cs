using Microsoft.AspNetCore.Components;

namespace JiraTime.Components;

/// <summary>
/// Shows modals in this tab and hands back their results. <see cref="ModalHost"/> (in MainLayout) draws
/// the open ones; <see cref="ModalQueue"/> decides which are open.
/// </summary>
public sealed class Modals
{
    private readonly ModalQueue _queue = new();
    private int _lastId;

    /// <summary>Raised when a modal opens, closes or changes (busy), so the host draws it again.</summary>
    public event Action? Changed;

    /// <summary>The open modals, the lowest first.</summary>
    public IReadOnlyList<ModalContext> Open => _queue.Open;

    /// <summary>
    /// Shows <typeparamref name="TModal"/> and waits until it closes. Returns its result, or null (the
    /// default) when it was cancelled. A modal asked for while one is open waits its turn, unless
    /// <paramref name="onTop"/> puts it above the open one (for a modal opened from inside a modal).
    /// </summary>
    public async Task<TResult?> ShowAsync<TModal, TResult>(IDictionary<string, object?>? parameters = null, bool onTop = false)
        where TModal : IComponent
    {
        var modal = new ModalContext(++_lastId, typeof(TModal), parameters ?? new Dictionary<string, object?>()) { Owner = this };
        _queue.Add(modal, onTop);
        Changed?.Invoke();
        return await modal.Result is TResult result ? result : default;
    }

    /// <summary>The open modal with this id; null once it closed.</summary>
    public ModalContext? Find(int id) => _queue.Open.FirstOrDefault(m => m.Id == id);

    /// <summary>Cancels every open and waiting modal.</summary>
    public void CloseAll()
    {
        if (_queue.Open.Count == 0) return;
        _queue.CloseAll();
        Changed?.Invoke();
    }

    internal void Close(ModalContext modal, object? result)
    {
        if (_queue.Close(modal, result)) Changed?.Invoke();
    }

    internal void Refresh() => Changed?.Invoke();
}
