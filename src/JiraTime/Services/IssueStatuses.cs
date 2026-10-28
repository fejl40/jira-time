using JiraTime.Domain;

namespace JiraTime.Services;

/// <summary>
/// The current Jira status of the issues a timesheet shows, for the "isn't to do any more" note. Filled
/// from the issue lists and looked up for the rest. Jira being down just means fewer statuses.
/// </summary>
public sealed class IssueStatuses(JiraIssueLookup issues, CurrentUser user)
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
        if (user.Connection is not { } c) return;
        var unknown = keys.Where(k => k != "" && !_known.ContainsKey(k)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (unknown.Count == 0) return;
        try
        {
            Add((await issues.GetStatusesAsync(c, unknown)).Values);
        }
        catch (JiraException)
        {
            // No status means no note; the time can still be logged.
        }
    }
}
