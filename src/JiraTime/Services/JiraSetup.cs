using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>Checks a new Jira connection and takes the token's own user as the profile.</summary>
public class JiraSetup(JiraClient jira, SettingsService settings)
{
    /// <summary>
    /// Asks Jira who the token belongs to and saves that user as the profile. A token always belongs to
    /// exactly one user, so there is nothing to choose.
    /// </summary>
    /// <exception cref="JiraException">The URL or token is wrong, or Jira can't be reached.</exception>
    public async Task<JiraProfile> ConnectAsync(JiraConnection c, CancellationToken ct = default)
    {
        var me = await jira.GetMyselfAsync(c, ct);
        await settings.SaveProfileAsync(me);
        return me;
    }
}
