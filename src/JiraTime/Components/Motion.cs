namespace JiraTime.Components;

/// <summary>Helpers for the animations in Styles/motion.css.</summary>
public static class Motion
{
    /// <summary>Style that staggers an animation: the <paramref name="order"/>th item starts a step later.</summary>
    public static string Order(int order) => $"--motion-order: {Math.Max(0, order)}";
}
