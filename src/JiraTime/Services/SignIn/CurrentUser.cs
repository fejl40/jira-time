using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>The signed-in user's way into Jira: the verified connection and the profile Jira returned for its token.</summary>
public sealed record SignedInJira(JiraConnection Connection, JiraProfile Profile);

/// <summary>
/// The verified user of this browser tab (one per circuit). Set only by <see cref="JiraIdentity"/>
/// after Jira confirmed the token; every data service reads its owner id from here.
/// </summary>
public sealed class CurrentUser
{
    public UserKey? Key { get; private set; }

    /// <summary>
    /// Connection and profile together; null when nobody is signed in. Every service that talks to Jira reads
    /// them from here. The connection carries the owner, so its Jira requests are logged under this user.
    /// </summary>
    public SignedInJira? Jira { get; private set; }

    /// <summary>True when Jira could not be reached and a token verified earlier was accepted.</summary>
    public bool VerifiedOffline { get; private set; }

    public bool IsSignedIn => Key is not null;

    /// <summary>Owner id for the user's documents.</summary>
    /// <exception cref="InvalidOperationException">Nobody is signed in.</exception>
    public string Owner => Key?.Id ?? throw new InvalidOperationException("No verified user in this session.");

    public event Action? Changed;

    /// <summary>Connection and profile for a request that can't go without them.</summary>
    /// <exception cref="JiraException">Nobody is signed in to Jira.</exception>
    public SignedInJira RequireJira() => Jira ?? throw new JiraException("Not connected to Jira.");

    /// <summary>Signs the tab in; only <see cref="JiraIdentity"/> calls this, once Jira confirmed the token.</summary>
    internal void Set(UserKey key, JiraProfile profile, JiraConnection connection, bool offline)
    {
        (Key, Jira, VerifiedOffline) = (key, new SignedInJira(connection with { Owner = key.Id }, profile), offline);
        JiraAction.Current?.Claim(key.Id); // requests made while signing in are this user's
        Changed?.Invoke();
    }

    public void Clear()
    {
        (Key, Jira, VerifiedOffline) = (null, null, false);
        Changed?.Invoke();
    }
}
