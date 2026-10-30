using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>
/// The issues a timesheet works with: their current Jira status, for the "isn't to do any more" note, and the
/// issue picker's Jira search. Statuses are filled from the issue lists and looked up for the rest; Jira being
/// down just means fewer statuses. One per timesheet page, shared with its parts.
/// </summary>
public sealed class IssueStatuses(JiraIssueLookup issues, JiraClient jira, CurrentUser user)
{
    private readonly Dictionary<string, IssueWithStatus> _known = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, IssueWithStatus> Known => _known;

    public void Add(IEnumerable<IssueWithStatus> found)
    {
        foreach (var i in found) _known[i.Key] = i;
    }

    /// <summary>Looks up the keys whose status isn't known yet.</summary>
    public async Task LookUpAsync(IEnumerable<string> keys)
    {
        if (user.Jira is not { } signedIn) return;
        var unknown = keys.Where(k => k != "" && !_known.ContainsKey(k)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (unknown.Count == 0) return;
        try
        {
            Add((await issues.GetStatusesAsync(signedIn.Connection, signedIn.Profile, unknown)).Values);
        }
        catch (JiraException)
        {
            // No status means no note; the time can still be logged.
        }
    }

    /// <summary>Issues Jira finds for what the user typed in the issue picker, logged as a search of their own.</summary>
    /// <exception cref="JiraException">Jira couldn't be asked, or nobody is signed in.</exception>
    public async Task<IReadOnlyList<JiraIssue>> SearchAsync(string query, CancellationToken ct = default)
    {
        using var action = JiraAction.Start($"Search issues “{query}”");
        return await jira.SearchIssuesAsync(user.RequireJira().Connection, query, ct);
    }
}
