namespace JiraTime.Services;

/// <summary>
/// The user action Jira requests are made for, such as "Save day Tue 27 Oct". The UI starts one around
/// what the user did; it flows with the async calls below it (AsyncLocal), so requests from parallel
/// actions and other tabs never mix. Requests outside any action are logged as Background.
/// </summary>
public sealed class JiraAction : IDisposable
{
    private static readonly AsyncLocal<JiraAction?> Ambient = new();

    private readonly Lock _lock = new();
    private readonly List<Action<string?>> _waitingForOwner = [];
    private bool _ended;

    public string Id { get; } = Guid.NewGuid().ToString("N")[..12];
    public string Name { get; }

    /// <summary>The user the action is for, once known while signing in; see <see cref="WhenOwnerKnown"/>.</summary>
    public string? Owner { get; private set; }

    private JiraAction(string name) => Name = name;

    /// <summary>The action the current code runs for, if any.</summary>
    public static JiraAction? Current => Ambient.Value;

    /// <summary>
    /// Starts an action until the result is disposed. Inside a running action this joins it instead, so a
    /// reload after Save day still counts as Save day.
    /// </summary>
    public static IDisposable Start(string name)
    {
        if (Ambient.Value is not null) return Joined.Instance;
        var action = new JiraAction(name);
        Ambient.Value = action;
        return action;
    }

    /// <summary>
    /// Calls <paramref name="use"/> with the owner once signing in found it (at once if known), or with null
    /// when the action ends without one. Requests made while signing in are logged this way.
    /// </summary>
    public void WhenOwnerKnown(Action<string?> use)
    {
        lock (_lock)
        {
            if (Owner is null && !_ended)
            {
                _waitingForOwner.Add(use);
                return;
            }
        }
        use(Owner);
    }

    /// <summary>Signing in confirmed whose token this is: the action is that user's.</summary>
    public void Claim(string owner)
    {
        lock (_lock) Owner ??= owner;
        Release();
    }

    public void Dispose()
    {
        if (Ambient.Value == this) Ambient.Value = null;
        lock (_lock) _ended = true;
        Release();
    }

    private void Release()
    {
        Action<string?>[] waiting;
        lock (_lock)
        {
            waiting = [.. _waitingForOwner];
            _waitingForOwner.Clear();
        }
        foreach (var use in waiting) use(Owner);
    }

    /// <summary>What a start inside a running action returns: disposing it leaves the action running.</summary>
    private sealed class Joined : IDisposable
    {
        public static readonly Joined Instance = new();
        public void Dispose() { }
    }
}
