using JiraTime.Domain;
using JiraTime.Storage;
using JiraTime.Services.SignIn;

namespace JiraTime.Services;

/// <summary>The signed-in user's export values per epic (Fase, Forordning, Hovedleverance).</summary>
public class EpicExportStore(IDocumentStore store, CurrentUser user)
{
    private readonly OwnedCollection<EpicExportValues> _values = new(store, user);

    /// <summary>The user's values for each epic that has any, by epic key.</summary>
    public async Task<IReadOnlyDictionary<string, EpicExportValues>> GetAsync(IEnumerable<string> epicKeys)
    {
        var found = new Dictionary<string, EpicExportValues>(StringComparer.OrdinalIgnoreCase);
        foreach (var epic in epicKeys.Distinct(StringComparer.OrdinalIgnoreCase))
            if (await _values.GetAsync(EpicExportValues.IdFor(_values.Owner, epic)) is { } v)
                found[epic] = v;
        return found;
    }

    /// <summary>Saves the values for <see cref="EpicExportValues.EpicKey"/>; all empty removes them.</summary>
    public async Task SaveAsync(EpicExportValues values)
    {
        var saved = new EpicExportValues
        {
            Id = EpicExportValues.IdFor(_values.Owner, values.EpicKey),
            EpicKey = values.EpicKey.ToUpperInvariant(),
            Fase = Clean(values.Fase),
            Forordning = Clean(values.Forordning),
            HovedleveranceNr = Clean(values.HovedleveranceNr),
            HovedleveranceLink = Clean(values.HovedleveranceLink),
        };
        if (saved.IsEmpty) await _values.DeleteAsync(saved.Id);
        else await _values.UpsertAsync(saved);
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
