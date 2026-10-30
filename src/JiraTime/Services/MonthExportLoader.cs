using JiraTime.Domain;
using JiraTime.Services.Jira;
using JiraTime.Services.SignIn;
using JiraTime.Services.Worklogs;

namespace JiraTime.Services;

/// <summary>A month of logged time, ready to export.</summary>
/// <param name="Rows">The export rows, every day of the month.</param>
/// <param name="Values">The user's values per epic in the month, by epic key; empty values for epics without any yet.</param>
/// <param name="NoEpic">Issues logged on that have no epic, by key.</param>
/// <param name="NotInJira">Entries in the month saved here but not in Jira yet; they are included.</param>
/// <param name="JiraError">Why Jira couldn't be read; then there is nothing to export.</param>
public sealed record LoadedExport(
    IReadOnlyList<ExportRow> Rows,
    IReadOnlyDictionary<string, EpicExportValues> Values,
    IReadOnlyList<string> NoEpic,
    int NotInJira,
    string? JiraError)
{
    public static LoadedExport Failed(string error) =>
        new([], new Dictionary<string, EpicExportValues>(), [], 0, error);
}

/// <summary>
/// Loads a month for the export: logged time (from Jira), the epic of each issue, and the user's values
/// per epic. The export needs all of it, so Jira failing for any part fails the load.
/// </summary>
public class MonthExportLoader(TimeLog log, JiraIssueLookup issues, EpicExportStore epicValues, CurrentUser user)
{
    /// <param name="month">Any day of the month.</param>
    /// <param name="refresh">Read Jira again instead of using worklogs read a moment ago.</param>
    public async Task<LoadedExport> LoadAsync(DateOnly month, bool refresh = false)
    {
        if (user.Jira is not { } signedIn) return LoadedExport.Failed("Not connected to Jira.");
        if (refresh) log.Refresh();

        var (from, to) = MonthExport.MonthOf(month);
        var logged = await log.GetRangeAsync(from, to);
        if (!logged.IsComplete) return LoadedExport.Failed(logged.JiraError!);

        var entries = logged.Between(from, to).Where(e => !e.DeletePending && e.Duration > TimeSpan.Zero).ToList();
        IReadOnlyDictionary<string, string?> epics;
        try
        {
            epics = await issues.GetEpicsAsync(signedIn.Connection, signedIn.Profile, entries.Select(e => e.IssueKey));
        }
        catch (JiraException ex)
        {
            return LoadedExport.Failed(ex.Message);
        }

        var rows = MonthExport.Rows(month, entries, epics);
        var values = await ValuesAsync(rows);
        return new LoadedExport(rows, values, MonthExport.IssuesWithoutEpic(rows), entries.Count(e => e.SyncPending), null);
    }

    /// <summary>The saved values of each epic in the rows, or new empty ones to fill in.</summary>
    private async Task<IReadOnlyDictionary<string, EpicExportValues>> ValuesAsync(IReadOnlyList<ExportRow> rows)
    {
        var epicKeys = rows.Select(r => r.EpicKey).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var saved = await epicValues.GetAsync(epicKeys);
        var values = new Dictionary<string, EpicExportValues>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in epicKeys)
            values[key] = saved.GetValueOrDefault(key) ?? new EpicExportValues { EpicKey = key.ToUpperInvariant() };
        return values;
    }
}
