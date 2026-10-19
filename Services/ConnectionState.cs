using System.Security.Cryptography;
using JiraTime.Domain;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace JiraTime.Services;

/// <summary>
/// The Jira connection (URL + token) for the current browser tab.
///
/// It is kept in the browser's localStorage, but encrypted by the server with
/// ASP.NET Core Data Protection (<see cref="ProtectedLocalStorage"/>). The key never
/// leaves the server (data/keys), so the stored value is useless on its own:
/// it can't be read by scripts, browser extensions or anyone copying the browser profile.
/// </summary>
public sealed class ConnectionState(ProtectedLocalStorage storage)
{
    private const string Purpose = "JiraTime.Connection.v1";
    private const string Key = "jira-time.connection";

    private bool _loaded;

    public JiraConnection? Connection { get; private set; }

    public event Action? Changed;

    /// <summary>Reads (and decrypts) the connection from the browser once per circuit.</summary>
    public async Task<JiraConnection?> LoadAsync()
    {
        if (_loaded) return Connection;
        try
        {
            var result = await storage.GetAsync<JiraConnection>(Purpose, Key);
            Connection = result.Success ? result.Value : null;
        }
        catch (CryptographicException)
        {
            // Server keys changed (e.g. data/keys deleted): the old value can't be decrypted.
            await storage.DeleteAsync(Key);
            Connection = null;
        }
        _loaded = true;
        return Connection;
    }

    public async Task SaveAsync(JiraConnection connection)
    {
        await storage.SetAsync(Purpose, Key, connection);
        Connection = connection;
        _loaded = true;
        Changed?.Invoke();
    }

    public async Task ClearAsync()
    {
        await storage.DeleteAsync(Key);
        Connection = null;
        _loaded = true;
        Changed?.Invoke();
    }
}
