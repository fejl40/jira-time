using System.Globalization;
using System.Reflection;
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
    /// Shows <typeparamref name="TModal"/> and waits until it closes and its dialog has left the page, so what the
    /// caller does next isn't hidden behind the fading backdrop. Returns its result, or null (the
    /// default) when it was cancelled. A modal asked for while one is open waits its turn, unless
    /// <paramref name="onTop"/> puts it above the open one (for a modal opened from inside a modal).
    /// </summary>
    public async Task<TResult?> ShowAsync<TModal, TResult>(IDictionary<string, object?>? parameters = null, bool onTop = false)
        where TModal : IComponent
    {
        var settings = ModalAttribute.For(typeof(TModal));
        var modal = new ModalContext(++_lastId, typeof(TModal), parameters ?? new Dictionary<string, object?>())
        {
            Size = settings.Size,
            Dismiss = settings.Dismiss,
            Owner = this,
        };
        _queue.Add(modal, onTop);
        Changed?.Invoke();
        var result = await modal.Result;
        await modal.Gone;
        return ModalResult.As<TResult>(result);
    }

    /// <summary>Asks a yes-or-no question; true when the user confirmed, false when they cancelled.</summary>
    /// <param name="danger">The answer deletes or loses something: the button is red and the focus starts on Cancel.</param>
    public Task<bool> ConfirmAsync(string title, string text, string confirmLabel = "OK", bool danger = false) =>
        ShowAsync<ConfirmModal, bool>(new Dictionary<string, object?>
        {
            [nameof(ConfirmModal.Title)] = title,
            [nameof(ConfirmModal.Text)] = text,
            [nameof(ConfirmModal.ConfirmLabel)] = confirmLabel,
            [nameof(ConfirmModal.Danger)] = danger,
        });

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

/// <summary>How wide a modal is: Small for a question, Medium for a short form, Large for a list.</summary>
public enum ModalSize { Small, Medium, Large }

/// <summary>A modal component's defaults: its width and whether Esc and a click on the backdrop cancel it.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ModalAttribute : Attribute
{
    public ModalSize Size { get; set; } = ModalSize.Medium;
    public bool Dismiss { get; set; } = true;

    /// <summary>The defaults of <paramref name="component"/>: its [Modal], or Medium and dismissable without one.</summary>
    public static ModalAttribute For(Type component) => component.GetCustomAttribute<ModalAttribute>() ?? new ModalAttribute();
}

/// <summary>Turns what a modal closed with into the result its caller asked for.</summary>
public static class ModalResult
{
    /// <summary>
    /// The value when it has the type asked for. Text from a data-modal-result button is read as the type
    /// (true, 4, an enum name). Anything else, and a cancelled modal, gives the default (null).
    /// </summary>
    public static TResult? As<TResult>(object? value)
    {
        if (value is TResult typed) return typed;
        if (value is not string text) return default;
        var type = Nullable.GetUnderlyingType(typeof(TResult)) ?? typeof(TResult);
        try
        {
            return (TResult)(type.IsEnum ? Enum.Parse(type, text, ignoreCase: true) : Convert.ChangeType(text, type, CultureInfo.InvariantCulture));
        }
        catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            return default;
        }
    }
}
