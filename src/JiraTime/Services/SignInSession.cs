using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>
/// Signing this browser tab in and out: the connection remembered in the browser
/// (<see cref="ConnectionState"/>) together with the identity Jira confirms for it (<see cref="JiraIdentity"/>).
/// Pages use this instead of coordinating the two.
/// </summary>
public sealed class SignInSession(ConnectionState stored, JiraIdentity identity)
{
    /// <summary>The connection remembered in this browser, to prefill the connect form.</summary>
    public Task<JiraConnection?> StoredConnectionAsync() => stored.LoadAsync();

    /// <summary>
    /// Signs in with the connection remembered in this browser. A token Jira rejects is forgotten, so the
    /// user connects again.
    /// </summary>
    /// <returns>Why signing in failed while a connection is still remembered; null otherwise (signed in, or nothing remembered).</returns>
    public async Task<string?> RestoreAsync()
    {
        if (await stored.LoadAsync() is not { } connection) return null;
        var result = await identity.SignInAsync(connection);
        if (result.Rejected)
        {
            await stored.ClearAsync();
            return null;
        }
        return result.SignedIn ? null : result.Error;
    }

    /// <summary>Signs in with a new connection. It is only remembered once Jira confirmed whose token it is.</summary>
    /// <returns>Why it failed, or null when signed in.</returns>
    public async Task<string?> ConnectAsync(JiraConnection connection)
    {
        var result = await identity.SignInAsync(connection, allowOffline: false);
        if (!result.SignedIn) return result.Error;
        await stored.SaveAsync(connection);
        return null;
    }

    /// <summary>Forgets the connection in this browser and signs out. Time entries stay on the server.</summary>
    public async Task SignOutAsync()
    {
        await stored.ClearAsync();
        identity.SignOut();
    }
}
