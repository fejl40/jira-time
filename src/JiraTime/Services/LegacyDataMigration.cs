using JiraTime.Domain;
using JiraTime.Storage;

namespace JiraTime.Services;

/// <summary>
/// Moves data saved by older versions of the app. Data saved before multi-user support has no owner. The first time its user signs in, it is given
/// to them, after a backup copy is stored. The user is the one recorded in the old profile setting;
/// if none was recorded, whoever signs in first (there was only one user then). Time entries from
/// before Jira became the source of truth are dropped once Jira has them (<see cref="RetireSyncedEntriesAsync"/>),
/// and entries saved with start and end times get them as hours.
/// </summary>
public class LegacyDataMigration(IDocumentStore store, TimeProvider clock)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private readonly IDocumentCollection<TimeEntry> _entries = store.Collection<TimeEntry>();
    private readonly IDocumentCollection<DayOff> _daysOff = store.Collection<DayOff>();
    private readonly IDocumentCollection<AppSettings> _settings = store.Collection<AppSettings>();
    private readonly IDocumentCollection<LegacyBackup> _backups = store.Collection<LegacyBackup>();
    private readonly IDocumentCollection<TimeEntriesBackup> _timeBackups = store.Collection<TimeEntriesBackup>();
    private readonly IDocumentCollection<TimedEntry> _timedEntries = store.Collection<TimedEntry>();

    /// <summary>Gives old data to <paramref name="user"/>; entries saved with start and end times get their hours first.</summary>
    /// <returns>True when old data was given to <paramref name="user"/>.</returns>
    public async Task<bool> ClaimAsync(UserKey user)
    {
        await Gate.WaitAsync();
        try
        {
            await GiveStartEndTimesHoursAsync();
            var entries = await _entries.QueryAsync(e => e.Owner == null || e.Owner == "");
            var daysOff = await _daysOff.QueryAsync(d => d.Owner == null || d.Owner == "");
            if (entries.Count == 0 && daysOff.Count == 0) return false;

            var settings = await _settings.GetAsync(AppSettings.SingletonId);
            var oldUser = settings?.Profile?.Id;
            if (!string.IsNullOrEmpty(oldUser) && oldUser != user.JiraUserId) return false;

            // Keep an untouched copy before changing anything.
            await _backups.UpsertAsync(new LegacyBackup
            {
                Id = $"before-multi-user-{clock.GetUtcNow():yyyyMMdd-HHmmss}-{user.Id[..8]}",
                CreatedUtc = clock.GetUtcNow().UtcDateTime,
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

    /// <summary>
    /// Entries saved before TASK-12 had start and end times instead of hours. Each gets the hours between
    /// them and is written back without the times. Entries that already have hours are left alone.
    /// </summary>
    private async Task GiveStartEndTimesHoursAsync()
    {
        var timed = await _timedEntries.QueryAsync(e => e.Start != null && e.End != null);
        foreach (var t in timed)
        {
            if (t.End <= t.Start || await _entries.GetAsync(t.Id) is not { } entry || entry.Duration != TimeSpan.Zero) continue;
            entry.Duration = t.End!.Value - t.Start!.Value;
            await _entries.UpsertAsync(entry);
        }
    }

    /// <summary>
    /// Logged time used to be kept here as well as in Jira. Jira is the source of truth now, so the user's
    /// entries Jira already has are dropped, after a backup copy of all their entries is stored. Entries
    /// Jira doesn't have yet stay: they are the retry queue and are sent by Sync now. Safe to run again.
    /// </summary>
    /// <returns>How many entries were dropped.</returns>
    public async Task<int> RetireSyncedEntriesAsync(UserKey user)
    {
        await Gate.WaitAsync();
        try
        {
            var owner = user.Id;
            var all = await _entries.QueryAsync(e => e.Owner == owner);
            var inJira = all.Where(e => e.IsSynced && !e.DeletePending).ToList();
            if (inJira.Count == 0) return 0;

            await _timeBackups.UpsertAsync(new TimeEntriesBackup
            {
                Id = $"time-entries-{clock.GetUtcNow():yyyyMMdd-HHmmss}-{owner[..8]}",
                CreatedUtc = clock.GetUtcNow().UtcDateTime,
                Owner = owner,
                Entries = all.Select(e => e.Copy()).ToList(),
            });
            foreach (var e in inJira) await _entries.DeleteAsync(e.Id);
            return inJira.Count;
        }
        finally
        {
            Gate.Release();
        }
    }
}

/// <summary>
/// The single settings document from before multi-user support. Only read to find out whose the old
/// data is; the profile now comes from Jira on each visit.
/// </summary>
[DocumentCollection("settings")]
public class AppSettings : IDocument
{
    public const string SingletonId = "app";
    public string Id { get; set; } = SingletonId;

    public JiraProfile? Profile { get; set; }
}

/// <summary>Copy of the data from before multi-user support, stored before it was given to its owner.</summary>
[DocumentCollection("backups")]
public class LegacyBackup : IDocument
{
    public string Id { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
    public UserKey? ClaimedBy { get; set; }
    public List<TimeEntry> Entries { get; set; } = [];
    public List<DayOff> DaysOff { get; set; } = [];
    public AppSettings? Settings { get; set; }
}

/// <summary>
/// Copy of a user's time entries from when the app kept logged time itself, stored before the entries
/// Jira already had were dropped (Jira is the source of truth now).
/// </summary>
[DocumentCollection("time-entry-backups")]
public class TimeEntriesBackup : IDocument
{
    public string Id { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
    public string Owner { get; set; } = "";
    public List<TimeEntry> Entries { get; set; } = [];
}

/// <summary>A time entry as saved before TASK-12, with start and end times; only read to give the entry its hours.</summary>
[DocumentCollection("time-entries")]
public class TimedEntry : IDocument
{
    public string Id { get; set; } = "";
    public TimeOnly? Start { get; set; }
    public TimeOnly? End { get; set; }
}
