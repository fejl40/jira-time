using System.Security.Cryptography;
using JiraTime.Domain;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using JiraTime.Services.JiraLog;

namespace JiraTime.Services.SignIn;

/// <summary>
/// Signing this browser tab in and out: the connection remembered in the browser together with the
/// identity Jira confirms for it (<see cref="JiraIdentity"/>). Pages use this instead of coordinating the two.
///
/// The connection (URL + token) is kept in the browser's localStorage, but encrypted by the server with
/// ASP.NET Core Data Protection (<see cref="ProtectedLocalStorage"/>). The key never leaves the server
/// (data/keys), so the stored value is useless on its own: it can't be read by scripts, browser
/// extensions or anyone copying the browser profile.
///
/// The browser's time zone is read at each sign-in and kept on the connection the tab signs in with, so today and
/// worklog start times are on the user's clock, also when the server runs in another zone.
/// </summary>
public sealed class SignInSession(ProtectedLocalStorage storage, JiraIdentity identity, IJSRuntime js)
{
    private const string Purpose = "JiraTime.Connection.v1";
    private const string StorageKey = "jira-time.connection";

    private bool _loaded;
    private JiraConnection? _stored;

    /// <summary>The connection remembered in this browser, to prefill the connect form. Read once per circuit.</summary>
    public async Task<JiraConnection?> StoredConnectionAsync()
    {
        if (_loaded) return _stored;
        try
        {
            var result = await storage.GetAsync<JiraConnection>(Purpose, StorageKey);
            _stored = result.Success ? result.Value : null;
        }
        catch (CryptographicException)
        {
            // Server keys changed (e.g. data/keys deleted): the old value can't be decrypted.
            await storage.DeleteAsync(StorageKey);
            _stored = null;
        }
        _loaded = true;
        return _stored;
    }

    /// <summary>
    /// Signs in with the connection remembered in this browser. A token Jira rejects is forgotten, so the
    /// user connects again.
    /// </summary>
    /// <returns>Why signing in failed while a connection is still remembered; null otherwise (signed in, or nothing remembered).</returns>
    public async Task<string?> RestoreAsync()
    {
        if (await StoredConnectionAsync() is not { } connection) return null;
        using var action = JiraAction.Start("Sign in");
        var result = await identity.SignInAsync(connection with { TimeZone = await BrowserTimeZoneAsync() });
        if (result.Rejected)
        {
            await ForgetAsync();
            return null;
        }
        return result.SignedIn ? null : result.Error;
    }

    /// <summary>Signs in with a new connection. It is only remembered once Jira confirmed whose token it is.</summary>
    /// <returns>Why it failed, or null when signed in.</returns>
    public async Task<string?> ConnectAsync(JiraConnection connection)
    {
        using var action = JiraAction.Start("Connect to Jira");
        var result = await identity.SignInAsync(connection with { TimeZone = await BrowserTimeZoneAsync() }, allowOffline: false);
        if (!result.SignedIn) return result.Error;
        await storage.SetAsync(Purpose, StorageKey, connection);
        (_stored, _loaded) = (connection, true);
        return null;
    }

    /// <summary>Forgets the connection in this browser and signs out. Time entries stay on the server.</summary>
    public async Task SignOutAsync()
    {
        await ForgetAsync();
        identity.SignOut();
    }

    /// <summary>The browser's time zone; null (the server's zone) when the browser gives none or one the server doesn't know.</summary>
    private async Task<TimeZoneInfo?> BrowserTimeZoneAsync()
    {
        string? name;
        try
        {
            name = await js.InvokeAsync<string?>("jiraTimeZone.name");
        }
        catch (JSException)
        {
            return null; // an old cached page without the script
        }
        return TimeZoneNamed(name);
    }

    /// <summary>The time zone with this IANA (or Windows) name; null when there is none or the server doesn't know it.</summary>
    internal static TimeZoneInfo? TimeZoneNamed(string? name) =>
        !string.IsNullOrWhiteSpace(name) && TimeZoneInfo.TryFindSystemTimeZoneById(name, out var zone) ? zone : null;

    private async Task ForgetAsync()
    {
        await storage.DeleteAsync(StorageKey);
        (_stored, _loaded) = (null, true);
    }
}
