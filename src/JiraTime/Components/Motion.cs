using JiraTime.Components.Sheet;
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

    /// <summary>
    /// Plays what follows Save all (wwwroot/flight.js): saved lines fly from the modal into their days, the saved
    /// weeks ripple, then a check mark flies from every cell marked data-sent to the period status.
    /// </summary>
    public Task PlaySaveAsync(SaveChoreography steps) => Call("jiraFlight.land", steps, SaveChoreography.Flight,
        SaveChoreography.Pulse, SaveChoreography.EndsBy, "[data-sent]", "[data-period-status]");

    /// <summary>The lines lifted from the Save all modal fade where they are: nothing was saved.</summary>
    public Task DropSaveAsync() => PlaySaveAsync(new SaveChoreography([], [], false));

    /// <summary>
    /// The finish for a complete period (wwwroot/finish.js): a light sweeps across the days and the background glows,
    /// then confetti. Once per user and period in this browser.
    /// </summary>
    public Task CelebrateAsync(UserKey user, Period period) =>
        Call("jiraFinish.celebrate", $"{user.Id}:{Format.Iso(period.Start)}", "[data-period-complete]", PeriodFinish.For(period.Length));

    /// <summary>A week ring that just closed pops and throws sparks.</summary>
    public Task SparkleAsync(string selector) => Call("jiraMotion.sparkle", selector);

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
