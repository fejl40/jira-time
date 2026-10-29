using JiraTime.Domain;
using JiraTime.Storage;

namespace JiraTime.Services;

/// <summary>
/// Which days need hours and how many: the user's work hours per weekday, minus Danish public
/// holidays and time the user marked off.
/// </summary>
public class WorkCalendarService(IDocumentStore store, CurrentUser user)
{
    private readonly IDocumentCollection<DayOff> _daysOff = store.Collection<DayOff>();
    private readonly IDocumentCollection<UserSettings> _settings = store.Collection<UserSettings>();

    public async Task<IReadOnlyDictionary<DateOnly, DayInfo>> GetDaysAsync(DateOnly from, DateOnly to)
    {
        var owner = user.Owner;
        var off = (await _daysOff.QueryAsync(d => d.Owner == owner && d.Date >= from && d.Date <= to)).ToDictionary(d => d.Date);
        var settings = await GetSettingsAsync();
        return Enumerable.Range(0, to.DayNumber - from.DayNumber + 1)
            .Select(from.AddDays)
            .ToDictionary(d => d, d => DanishHolidays.Classify(d, off, settings));
    }

    /// <summary>The user's settings; the standard week when they never changed them.</summary>
    public async Task<UserSettings> GetSettingsAsync() =>
        await user.GetMineAsync(_settings, user.Owner) ?? new UserSettings { Id = user.Owner, Owner = user.Owner };

    /// <returns>Why the hours were not saved, or null.</returns>
    public async Task<string?> SaveWorkHoursAsync(IReadOnlyDictionary<DayOfWeek, TimeSpan> hours)
    {
        if (UserSettings.Validate(hours) is { } error) return error;
        var settings = await GetSettingsAsync();
        settings.WorkHours = Enum.GetValues<DayOfWeek>().ToDictionary(d => d, d => hours.GetValueOrDefault(d));
        await _settings.UpsertAsync(settings);
        return null;
    }

    public async Task<DayInfo> GetDayAsync(DateOnly date) => (await GetDaysAsync(date, date))[date];

    /// <param name="hours">Hours off; null for the whole day.</param>
    public Task MarkDayOffAsync(DateOnly date, DayOffReason reason, TimeSpan? hours = null) =>
        _daysOff.UpsertAsync(new DayOff
        {
            Id = DayOff.IdFor(user.Owner, date), Owner = user.Owner, Date = date, Reason = reason,
            Hours = hours > TimeSpan.Zero ? hours : null,
        });

    public Task UnmarkDayOffAsync(DateOnly date) => _daysOff.DeleteAsync(DayOff.IdFor(user.Owner, date));
}
