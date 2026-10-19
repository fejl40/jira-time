using JiraTime.Domain;
using JiraTime.Storage;

namespace JiraTime.Services;

/// <summary>Which days need hours: weekdays, minus Danish public holidays and days the user marked off.</summary>
public class WorkCalendarService(IDocumentStore store, CurrentUser user)
{
    private readonly IDocumentCollection<DayOff> _daysOff = store.Collection<DayOff>();

    public async Task<IReadOnlyDictionary<DateOnly, DayInfo>> GetDaysAsync(DateOnly from, DateOnly to)
    {
        var owner = user.Owner;
        var off = (await _daysOff.QueryAsync(d => d.Owner == owner && d.Date >= from && d.Date <= to)).ToDictionary(d => d.Date);
        return Enumerable.Range(0, to.DayNumber - from.DayNumber + 1)
            .Select(from.AddDays)
            .ToDictionary(d => d, d => DanishHolidays.Classify(d, off));
    }

    public async Task<DayInfo> GetDayAsync(DateOnly date) => (await GetDaysAsync(date, date))[date];

    public Task MarkDayOffAsync(DateOnly date, DayOffReason reason) =>
        _daysOff.UpsertAsync(new DayOff { Id = DayOff.IdFor(user.Owner, date), Owner = user.Owner, Date = date, Reason = reason });

    public Task UnmarkDayOffAsync(DateOnly date) => _daysOff.DeleteAsync(DayOff.IdFor(user.Owner, date));

    /// <summary>Hours still missing on a day: target minus logged, never negative. Zero on non-workdays.</summary>
    public static TimeSpan Missing(DayInfo day, TimeSpan logged) =>
        day.Expected > logged ? day.Expected - logged : TimeSpan.Zero;
}
