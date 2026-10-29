using System.Net;
using JiraTime.Domain;
using JiraTime.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.JSInterop;

namespace JiraTime.Tests;

/// <summary>Signing a tab in and out: what is remembered in the browser follows what Jira says.</summary>
public class SignInSessionTests : IDisposable
{
    private static readonly JiraConnection Server = TimeHarness.Server with { Token = "token-1" };

    private readonly TimeHarness _h = new();
    private readonly FakeLocalStorage _browser = new();
    private readonly EphemeralDataProtectionProvider _keys = new();
    private readonly CurrentUser _user = new();

    public SignInSessionTests() =>
        _h.Jira.Gets["/rest/api/2/myself"] = _ => new { key = "JIRAUSER1", name = "carl", displayName = "Carl Lilholm" };

    public void Dispose() => _h.Dispose();

    /// <summary>A new tab (circuit) over the same browser storage, server keys and store.</summary>
    private SignInSession NewTab() => new(new ProtectedLocalStorage(_browser, _keys), _h.Identity(_user));

    [Fact]
    public async Task A_confirmed_connection_is_remembered_and_restores_in_a_new_tab()
    {
        Assert.Null(await NewTab().ConnectAsync(Server));

        Assert.True(_user.IsSignedIn);
        Assert.DoesNotContain("token-1", Assert.Single(_browser.Items).Value); // encrypted
        _user.Clear();
        var tab = NewTab();
        Assert.Null(await tab.RestoreAsync());
        Assert.True(_user.IsSignedIn);
        Assert.Equal(Server, await tab.StoredConnectionAsync());
    }

    [Fact]
    public async Task A_connection_Jira_does_not_confirm_is_not_remembered()
    {
        _h.Jira.FailWith = HttpStatusCode.Unauthorized;

        Assert.NotNull(await NewTab().ConnectAsync(Server));

        Assert.False(_user.IsSignedIn);
        Assert.Empty(_browser.Items);
    }

    [Fact]
    public async Task A_remembered_token_Jira_rejects_is_forgotten()
    {
        await NewTab().ConnectAsync(Server);
        _h.Jira.FailWith = HttpStatusCode.Unauthorized;
        var tab = NewTab();

        Assert.Null(await tab.RestoreAsync());

        Assert.False(_user.IsSignedIn);
        Assert.Empty(_browser.Items);
        Assert.Null(await tab.StoredConnectionAsync());
    }

    [Fact]
    public async Task Signing_out_forgets_the_connection()
    {
        var tab = NewTab();
        await tab.ConnectAsync(Server);

        await tab.SignOutAsync();

        Assert.False(_user.IsSignedIn);
        Assert.Empty(_browser.Items);
        Assert.Null(await NewTab().StoredConnectionAsync());
    }

    /// <summary>The browser's localStorage, answering the calls <see cref="ProtectedLocalStorage"/> makes.</summary>
    private sealed class FakeLocalStorage : IJSRuntime
    {
        public Dictionary<string, string> Items { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            var key = (string)args![0]!;
            object? result = null;
            switch (identifier)
            {
                case "localStorage.getItem": result = Items.GetValueOrDefault(key); break;
                case "localStorage.setItem": Items[key] = (string)args[1]!; break;
                case "localStorage.removeItem": Items.Remove(key); break;
                default: throw new NotSupportedException(identifier);
            }
            return ValueTask.FromResult((TValue)result!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }
}
