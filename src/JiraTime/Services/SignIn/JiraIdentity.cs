using System.Net;
using JiraTime.Domain;
using JiraTime.Storage;
using JiraTime.Services.Jira;

namespace JiraTime.Services.SignIn;

/// <summary>
/// Signs a browser tab in as the Jira user its token belongs to. The identity is whatever the chosen
/// Jira host answers for /myself, never something the browser claims.
/// </summary>
public class JiraIdentity(JiraClient jira, IDocumentStore store, CurrentUser user, LegacyDataMigration legacy, TimeProvider clock)
{
    private readonly IDocumentCollection<UserAccount> _accounts = store.Collection<UserAccount>();

    public record Result(bool SignedIn, string? Error, bool Rejected)
    {
        public static readonly Result Ok = new(true, null, false);
    }

    /// <summary>
    /// Asks Jira who the token belongs to and signs in as that user. If Jira can't be reached and
    /// <paramref name="allowOffline"/> is set, a token Jira confirmed before for this host is accepted.
    /// </summary>
    public async Task<Result> SignInAsync(JiraConnection c, bool allowOffline = true, CancellationToken ct = default)
    {
        JiraProfile me;
        try
        {
            me = await jira.GetMyselfAsync(c, ct);
        }
        catch (JiraException ex)
        {
            return await NotConfirmedAsync(c, ex, allowOffline);
        }

        if (string.IsNullOrEmpty(me.Id))
        {
            user.Clear();
            return new Result(false, "Jira did not say who the token belongs to.", Rejected: false);
        }

        var key = UserKey.For(c.BaseUrl, me.Id);
        var now = clock.GetUtcNow().UtcDateTime;
        var account = await _accounts.GetAsync(key.Id)
            ?? new UserAccount { Id = key.Id, Host = key.Host, JiraUserId = key.JiraUserId, CreatedUtc = now };
        account.Confirm(me, UserAccount.TokenHash(c), now);
        await _accounts.UpsertAsync(account);

        await legacy.ClaimAsync(key);
        await legacy.RetireSyncedEntriesAsync(key);
        user.Set(key, me, c, offline: false);
        return Result.Ok;
    }

    /// <summary>
    /// Jira didn't confirm the token. A token it rejects is forgotten; when Jira can't be reached, a token
    /// it confirmed before still signs in (offline) if <paramref name="allowOffline"/> is set.
    /// </summary>
    private async Task<Result> NotConfirmedAsync(JiraConnection c, JiraException ex, bool allowOffline)
    {
        if (ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            await ForgetTokenAsync(c);
            user.Clear();
            return new Result(false, ex.Message, Rejected: true);
        }
        if (allowOffline && IsUnreachable(ex) && await FindByTokenAsync(c) is { } known)
        {
            user.Set(known.Key, known.Profile, c, offline: true);
            return Result.Ok;
        }
        user.Clear();
        return new Result(false, ex.Message, Rejected: false);
    }

    public void SignOut() => user.Clear();

    /// <summary>Network errors, timeouts and server errors: Jira might be fine again later.</summary>
    private static bool IsUnreachable(JiraException ex) => ex.Status is null || (int)ex.Status >= 500;

    private async Task<UserAccount?> FindByTokenAsync(JiraConnection c)
    {
        var token = UserAccount.TokenHash(c);
        var host = UserKey.NormalizeHost(c.BaseUrl);
        return (await _accounts.QueryAsync(a => a.Host == host)).FirstOrDefault(a => a.VerifiedTokens.Contains(token));
    }

    private async Task ForgetTokenAsync(JiraConnection c)
    {
        if (await FindByTokenAsync(c) is not { } account) return;
        account.VerifiedTokens.Remove(UserAccount.TokenHash(c));
        await _accounts.UpsertAsync(account);
    }
}
