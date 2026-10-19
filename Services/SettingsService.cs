using JiraTime.Domain;
using JiraTime.Storage;

namespace JiraTime.Services;

/// <summary>Server-side settings (profile, period start). Raises <see cref="Changed"/> so open pages can refresh.</summary>
public class SettingsService(IDocumentStore store)
{
    private readonly IDocumentCollection<AppSettings> _docs = store.Collection<AppSettings>();

    public event Action? Changed;

    public async Task<AppSettings> GetAsync() =>
        await _docs.GetAsync(AppSettings.SingletonId) ?? new AppSettings();

    public Task SaveProfileAsync(JiraProfile? p) => UpdateAsync(s => s.Profile = p);

    public Task SavePeriodAnchorAsync(DateOnly anchor) => UpdateAsync(s => s.PeriodAnchor = anchor);

    private async Task UpdateAsync(Action<AppSettings> change)
    {
        var s = await GetAsync();
        change(s);
        await _docs.UpsertAsync(s);
        Changed?.Invoke();
    }
}
