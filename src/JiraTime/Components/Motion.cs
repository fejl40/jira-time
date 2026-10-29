using JiraTime.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace JiraTime.Components;

/// <summary>Helpers for the animations in Styles/motion.css.</summary>
public static class Motion
{
    /// <summary>Style that staggers an animation: the <paramref name="order"/>th item starts a step later.</summary>
    public static string Order(int order) => $"--motion-order: {Math.Max(0, order)}";

    /// <summary>Class that slides content in: from the right for a later period (1), the left for an earlier one (-1).</summary>
    public static string Slide(int direction) => Math.Sign(direction) switch
    {
        > 0 => "motion-slide-next",
        < 0 => "motion-slide-previous",
        _ => "",
    };
}

/// <summary>
/// Calls the animations in wwwroot/motion.js. An animation never matters enough to fail a page, so a
/// closed tab or a script error is ignored.
/// </summary>
public sealed class MotionScript(IJSRuntime js)
{
    /// <summary>Rows the day editor shows as removed.</summary>
    public const string RemovedRows = "[data-row=removed]";

    /// <summary>Shrinks the matching rows away; returns when they are gone from sight.</summary>
    public Task CollapseAsync(string selector) => Call("jiraMotion.collapse", selector);

    /// <summary>Moves the grid's selected-day highlight to the selected column.</summary>
    public Task GlideAsync(ElementReference grid) => Call("jiraMotion.glide", grid);

    /// <summary>Sends a check mark from every cell marked data-sent to the period status.</summary>
    public Task FlyTicksAsync() => Call("jiraMotion.flyTicks", "[data-sent]", "[data-period-status]");

    /// <summary>Confetti for a complete period, once per user and period in this browser.</summary>
    public Task CelebrateAsync(UserKey user, Period period) =>
        Call("jiraMotion.celebrate", $"{user.Id}:{Format.Iso(period.Start)}", "[data-period-complete]");

    private async Task Call(string function, params object[] args)
    {
        try
        {
            await js.InvokeVoidAsync(function, args);
        }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
    }
}

/// <summary>The last save, for its animation: days saved, and those Jira got completely.</summary>
public sealed class SaveMotion
{
    private bool _flyTicks;

    public HashSet<DateOnly> JustSaved { get; } = [];
    public HashSet<DateOnly> SentToJira { get; } = [];

    /// <summary>A save begins: the last one's days no longer animate.</summary>
    public void Start()
    {
        JustSaved.Clear();
        SentToJira.Clear();
    }

    public void Saved(DateOnly day, bool sentToJira)
    {
        JustSaved.Add(day);
        if (sentToJira) SentToJira.Add(day);
    }

    /// <summary>Called after the saved work has reloaded, so the ticks start from the saved cells.</summary>
    public void Finish() => _flyTicks = SentToJira.Count > 0;

    /// <summary>True once after a save that sent days to Jira: time to fly the ticks.</summary>
    public bool TakeFlyTicks()
    {
        var fly = _flyTicks;
        _flyTicks = false;
        return fly;
    }
}
