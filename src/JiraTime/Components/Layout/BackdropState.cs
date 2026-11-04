using JiraTime.Domain;

namespace JiraTime.Components.Layout;

/// <summary>
/// The aurora's palette for this tab: the timesheet shows its period's progress, every other page the neutral
/// palette. BackdropLayer.razor listens, so only the background redraws when the palette changes.
/// </summary>
public sealed class BackdropState
{
    public AuroraPalette Palette { get; private set; } = AuroraPalette.Neutral;

    public event Action? Changed;

    /// <summary>Shows a period's progress; null (no period on screen) shows the neutral palette.</summary>
    public void Show(PeriodSummary? summary)
    {
        var palette = Backdrop.Palette(summary);
        if (palette == Palette) return;
        Palette = palette;
        Changed?.Invoke();
    }
}
