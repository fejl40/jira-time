using System.Globalization;
using JiraTime.Storage;

namespace JiraTime.Domain;

/// <summary>
/// The export columns that depend on the epic (Fase, Forordning, Hovedleverance), set once per epic by
/// the user (id = owner + epic key).
/// </summary>
[DocumentCollection("epic-export-values")]
public class EpicExportValues : IOwnedDocument
{
    public string Id { get; set; } = "";
    public string Owner { get; set; } = "";
    public string EpicKey { get; set; } = "";

    public string? Fase { get; set; }
    public string? Forordning { get; set; }
    public string? HovedleveranceNr { get; set; }
    public string? HovedleveranceLink { get; set; }

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Fase) && string.IsNullOrWhiteSpace(Forordning)
        && string.IsNullOrWhiteSpace(HovedleveranceNr) && string.IsNullOrWhiteSpace(HovedleveranceLink);

    public static string IdFor(string owner, string epicKey) => $"{owner}-{epicKey.ToUpperInvariant()}";
}

/// <summary>One line of the export: a day, or a day and an issue with its hours added up.</summary>
/// <param name="IssueKey">Null on a day with no time logged; the row then has only the date.</param>
/// <param name="EpicKey">The issue's epic; null when it has none.</param>
public record ExportRow(DateOnly Date, TimeSpan Hours = default, string? IssueKey = null, string? EpicKey = null);

/// <summary>
/// A calendar month of logged time in the agency's tab-separated format: every day of the month, one row
/// per day and issue, blank apart from the date on days with no time.
/// </summary>
public static class MonthExport
{
    public static readonly IReadOnlyList<string> Header =
    [
        "Dato (yyyy-mm-dd)", "Timer", "Fase (ydelsesområde)", "Forordning", "Hovedleverance nr.", "Hovedleverance link", "Epic", "Beskrivelse",
    ];

    /// <summary>First and last day of the month <paramref name="day"/> is in.</summary>
    public static (DateOnly From, DateOnly To) MonthOf(DateOnly day)
    {
        var from = new DateOnly(day.Year, day.Month, 1);
        return (from, from.AddMonths(1).AddDays(-1));
    }

    /// <param name="month">Any day of the month.</param>
    /// <param name="entries">Logged time; entries outside the month are ignored.</param>
    /// <param name="epics">Issue key → its epic (null or missing when it has none).</param>
    public static IReadOnlyList<ExportRow> Rows(DateOnly month, IEnumerable<TimeEntry> entries, IReadOnlyDictionary<string, string?> epics)
    {
        var (from, to) = MonthOf(month);
        var logged = entries.Where(e => !e.DeletePending && e.Date >= from && e.Date <= to && e.Duration > TimeSpan.Zero);
        var byDay = logged.GroupBy(e => e.Date).ToDictionary(g => g.Key, g => DayRows(g.Key, g, epics));

        var rows = new List<ExportRow>();
        for (var d = from; d <= to; d = d.AddDays(1))
            rows.AddRange(byDay.TryGetValue(d, out var day) ? day : [new ExportRow(d)]);
        return rows;
    }

    /// <summary>One row per issue on a day, by key, with its hours added up.</summary>
    private static List<ExportRow> DayRows(DateOnly date, IEnumerable<TimeEntry> entries, IReadOnlyDictionary<string, string?> epics)
    {
        var byIssue = entries.GroupBy(e => e.IssueKey.ToUpperInvariant()).OrderBy(i => i.Key, StringComparer.Ordinal);
        return byIssue.Select(i => new ExportRow(date, TimeEntry.Total(i), i.Key, epics.GetValueOrDefault(i.Key))).ToList();
    }

    /// <summary>The row's eight cells, in <see cref="Header"/> order.</summary>
    /// <param name="values">Epic key → the user's values for it.</param>
    public static IReadOnlyList<string> Cells(ExportRow row, string baseUrl, IReadOnlyDictionary<string, EpicExportValues> values)
    {
        if (row.IssueKey is null) return [row.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "", "", "", "", "", "", ""];
        var v = row.EpicKey is null ? null : values.GetValueOrDefault(row.EpicKey);
        return
        [
            row.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Hours(row.Hours),
            v?.Fase ?? "",
            v?.Forordning ?? "",
            v?.HovedleveranceNr ?? "",
            v?.HovedleveranceLink ?? "",
            row.EpicKey is null ? "" : IssueUrl(baseUrl, row.EpicKey),
            row.IssueKey,
        ];
    }

    /// <summary>Header and rows, tab-separated, one line each. Tabs and line breaks in values become spaces.</summary>
    public static string ToTsv(IEnumerable<ExportRow> rows, string baseUrl, IReadOnlyDictionary<string, EpicExportValues> values) =>
        string.Join("\r\n", rows.Select(r => Cells(r, baseUrl, values)).Prepend(Header).Select(Line)) + "\r\n";

    /// <summary>Hours with a decimal comma: 7,5 and 0,25.</summary>
    public static string Hours(TimeSpan hours) =>
        Math.Round(hours.TotalHours, 2).ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');

    public static string IssueUrl(string baseUrl, string key) => $"{baseUrl.TrimEnd('/')}/browse/{key}";

    private static string Line(IEnumerable<string> cells) =>
        string.Join('\t', cells.Select(c => c.Replace("\r\n", " ").Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ').Trim()));
}
