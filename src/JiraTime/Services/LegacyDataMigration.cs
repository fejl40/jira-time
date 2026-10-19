using JiraTime.Domain;
using JiraTime.Storage;

namespace JiraTime.Services;

/// <summary>
/// Data saved before multi-user support has no owner. The first time its user signs in, it is given
/// to them, after a backup copy is stored. The user is the one recorded in the old profile setting;
/// if none was recorded, whoever signs in first (there was only one user then).
/// </summary>
public class LegacyDataMigration(IDocumentStore store)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private readonly IDocumentCollection<TimeEntry> _entries = store.Collection<TimeEntry>();
    private readonly IDocumentCollection<DayOff> _daysOff = store.Collection<DayOff>();
    private readonly IDocumentCollection<AppSettings> _settings = store.Collection<AppSettings>();
    private readonly IDocumentCollection<LegacyBackup> _backups = store.Collection<LegacyBackup>();

    /// <returns>True when old data was given to <paramref name="user"/>.</returns>
    public async Task<bool> ClaimAsync(UserKey user)
    {
        await Gate.WaitAsync();
        try
        {
            var entries = await _entries.QueryAsync(e => e.Owner == null || e.Owner == "");
            var daysOff = await _daysOff.QueryAsync(d => d.Owner == null || d.Owner == "");
            if (entries.Count == 0 && daysOff.Count == 0) return false;

            var settings = await _settings.GetAsync(AppSettings.SingletonId);
            var oldUser = settings?.Profile?.Id;
            if (!string.IsNullOrEmpty(oldUser) && oldUser != user.JiraUserId) return false;

            // Keep an untouched copy before changing anything.
            await _backups.UpsertAsync(new LegacyBackup
            {
                Id = $"before-multi-user-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{user.Id[..8]}",
                ClaimedBy = user,
                Entries = entries.Select(e => e.Copy()).ToList(),
                DaysOff = daysOff.Select(d => new DayOff { Id = d.Id, Date = d.Date, Reason = d.Reason }).ToList(),
                Settings = settings,
            });

            foreach (var e in entries)
            {
                var owned = e.Copy();
                owned.Owner = user.Id;
                await _entries.UpsertAsync(owned);
            }
            foreach (var d in daysOff)
            {
                await _daysOff.UpsertAsync(new DayOff { Id = DayOff.IdFor(user.Id, d.Date), Owner = user.Id, Date = d.Date, Reason = d.Reason });
                if (d.Id != DayOff.IdFor(user.Id, d.Date)) await _daysOff.DeleteAsync(d.Id);
            }
            return true;
        }
        finally
        {
            Gate.Release();
        }
    }
}
