using JiraTime.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace JiraTime.Components;

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
