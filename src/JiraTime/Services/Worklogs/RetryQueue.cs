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
    private readonly OwnedCollection<TimeEntry> _changes = new(store, user);

    /// <summary>Waiting changes on days in the range, in day order.</summary>
    public async Task<IReadOnlyList<TimeEntry>> GetPendingAsync(DateOnly from, DateOnly to)
    {
        var inRange = await _changes.QueryAsync(e => e.Date >= from && e.Date <= to);
        return inRange.Where(e => !e.IsSynced).OrderBy(e => e.Date).ThenBy(e => e.CreatedUtc).ToList();
    }

    /// <summary>Puts the change in the queue, or replaces the one with its id.</summary>
    /// <exception cref="InvalidOperationException">The id belongs to someone else's change.</exception>
    public Task AddAsync(TimeEntry entry) => _changes.UpsertAsync(entry);

    /// <summary>Takes the change out of the queue, if it is the signed-in user's.</summary>
    public Task RemoveAsync(string id) => _changes.DeleteAsync(id);
}
