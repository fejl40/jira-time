namespace JiraTime.Domain;

/// <summary>
/// Notices things that become reached while the user watches, like a day that becomes complete. What
/// was reached at the first look doesn't count. Pure logic, for celebrating at the right moment.
/// </summary>
public sealed class Milestones<T> where T : notnull
{
    private HashSet<T>? _reached;
    private readonly HashSet<T> _fresh = [];

    /// <summary>Things reached since the first look and still reached.</summary>
    public IReadOnlySet<T> Fresh => _fresh;

    /// <summary>Takes what is reached now.</summary>
    /// <returns>What became reached with this call.</returns>
    public IReadOnlySet<T> Update(IEnumerable<T> reached)
    {
        var now = reached.ToHashSet();
        var newly = _reached is null ? [] : now.Where(x => !_reached.Contains(x)).ToHashSet();
        _reached = now;
        _fresh.IntersectWith(now);
        _fresh.UnionWith(newly);
        return newly;
    }
}
