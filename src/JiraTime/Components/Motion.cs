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
