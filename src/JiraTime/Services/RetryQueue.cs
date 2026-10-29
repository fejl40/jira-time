using JiraTime.Domain;
using JiraTime.Storage;

namespace JiraTime.Services;

/// <summary>
/// The signed-in user's changes Jira couldn't take yet: new or changed work, and removals still to send.
/// The only logged time stored locally; each change leaves the queue once Jira has it
/// (<see cref="WorklogSync"/>). Every read and write is limited to <see cref="CurrentUser"/>.
/// </summary>
public class RetryQueue(IDocumentStore store, CurrentUser user)
{
    private readonly IDocumentCollection<TimeEntry> _docs = store.Collection<TimeEntry>();

    /// <summary>Waiting changes on days in the range, in day order.</summary>
    public async Task<IReadOnlyList<TimeEntry>> GetPendingAsync(DateOnly from, DateOnly to)
    {
        var owner = user.Owner;
        var mine = await _docs.QueryAsync(e => e.Owner == owner && e.Date >= from && e.Date <= to);
        return mine.Where(e => !e.IsSynced).OrderBy(e => e.Date).ThenBy(e => e.CreatedUtc).ToList();
    }

    /// <summary>Puts the change in the queue, or replaces the one with its id.</summary>
    /// <exception cref="InvalidOperationException">The id belongs to someone else's change.</exception>
    public async Task AddAsync(TimeEntry entry)
    {
        if (await _docs.GetAsync(entry.Id) is { } stored && !user.Owns(stored))
            throw new InvalidOperationException("This entry belongs to someone else.");
        entry.Owner = user.Owner;
        await _docs.UpsertAsync(entry);
    }

    /// <summary>Takes the change out of the queue, if it is the signed-in user's.</summary>
    public async Task RemoveAsync(string id)
    {
        if (await user.GetMineAsync(_docs, id) is not null) await _docs.DeleteAsync(id);
    }
}
