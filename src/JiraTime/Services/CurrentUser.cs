using JiraTime.Domain;
using JiraTime.Storage;

namespace JiraTime.Services;

/// <summary>
/// The verified user of this browser tab (one per circuit). Set only by <see cref="JiraIdentity"/>
/// after Jira confirmed the token; every data service reads its owner id from here.
/// </summary>
public sealed class CurrentUser
{
    public UserKey? Key { get; private set; }
    public JiraProfile? Profile { get; private set; }
    public JiraConnection? Connection { get; private set; }

    /// <summary>True when Jira could not be reached and a token verified earlier was accepted.</summary>
    public bool VerifiedOffline { get; private set; }

    public bool IsSignedIn => Key is not null;

    /// <summary>Owner id for the user's documents.</summary>
    /// <exception cref="InvalidOperationException">Nobody is signed in.</exception>
    public string Owner => Key?.Id ?? throw new InvalidOperationException("No verified user in this session.");

    public event Action? Changed;

    /// <summary>True when the document is this user's.</summary>
    /// <exception cref="InvalidOperationException">Nobody is signed in.</exception>
    public bool Owns(IOwnedDocument document) => document.Owner == Owner;

    /// <summary>The document with this id when it is this user's; null when there is none or it is someone else's.</summary>
    public async Task<T?> GetMineAsync<T>(IDocumentCollection<T> collection, string id) where T : class, IOwnedDocument =>
        await collection.GetAsync(id) is { } document && Owns(document) ? document : null;

    internal void Set(UserKey key, JiraProfile profile, JiraConnection connection, bool offline)
    {
        (Key, Profile, Connection, VerifiedOffline) = (key, profile, connection, offline);
        Changed?.Invoke();
    }

    public void Clear()
    {
        (Key, Profile, Connection, VerifiedOffline) = (null, null, null, false);
        Changed?.Invoke();
    }

    /// <summary>For tests: a signed-in user without going through Jira.</summary>
    public static CurrentUser SignedIn(UserKey key, JiraProfile? profile = null, JiraConnection? connection = null)
    {
        var user = new CurrentUser();
        user.Set(key, profile ?? new JiraProfile(key.JiraUserId, null, key.JiraUserId, null),
            connection ?? new JiraConnection($"https://{key.Host}", JiraAuthType.Pat, "token", null), offline: false);
        return user;
    }
}
