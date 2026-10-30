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

    public void Dispose() => _h.Dispose();

    /// <summary>A new tab (circuit) over the same browser storage, server keys and store.</summary>
    private SignInSession NewTab() => new(new ProtectedLocalStorage(_browser, _keys), _h.Identity(_user), _browser);

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

    [Theory]
    [InlineData("America/New_York")]
    [InlineData("Asia/Kolkata")]
    public async Task The_browsers_time_zone_is_kept_with_the_signed_in_user(string zone)
    {
        _browser.TimeZone = zone;

        await NewTab().ConnectAsync(Server);
        Assert.Equal(TimeZoneInfo.FindSystemTimeZoneById(zone).BaseUtcOffset, _user.TimeZone!.BaseUtcOffset);
        _user.Clear();
        await NewTab().RestoreAsync();

        Assert.Equal(TimeZoneInfo.FindSystemTimeZoneById(zone).BaseUtcOffset, _user.TimeZone!.BaseUtcOffset);
        Assert.Same(_user.TimeZone, _user.RequireJira().Connection.TimeZone); // worklogs start on the same clock
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Mars/Olympus_Mons")]
    public async Task No_or_an_unknown_browser_time_zone_leaves_the_servers(string? zone)
    {
        _browser.TimeZone = zone;

        await NewTab().ConnectAsync(Server);

        Assert.True(_user.IsSignedIn);
        Assert.Null(_user.TimeZone);
    }

    /// <summary>The browser's localStorage and time zone, answering the calls <see cref="ProtectedLocalStorage"/> and the sign-in make.</summary>
    private sealed class FakeLocalStorage : IJSRuntime
    {
        public Dictionary<string, string> Items { get; } = [];

        /// <summary>What Intl.DateTimeFormat says the browser's time zone is.</summary>
        public string? TimeZone { get; set; } = "Europe/Copenhagen";

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "jiraTimeZone.name") return ValueTask.FromResult((TValue)(object?)TimeZone!);
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
