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
    public string? Owner { get; set; } = "";
    public string EpicKey { get; set; } = "";

    public string? Fase { get; set; }
    public string? Forordning { get; set; }
    public string? HovedleveranceNr { get; set; }
    public string? HovedleveranceLink { get; set; }

    public bool IsEmpty => MonthExport.EpicColumns.All(column => string.IsNullOrWhiteSpace(column.Get(this)));

    public static string IdFor(string owner, string epicKey) => $"{owner}-{epicKey.ToUpperInvariant()}";
}

/// <summary>One line of the export: a day, or a day and an issue with its hours added up.</summary>
/// <param name="IssueKey">Null on a day with no time logged; the row then has only the date.</param>
/// <param name="EpicKey">The issue's epic; null when it has none.</param>
public record ExportRow(DateOnly Date, TimeSpan Hours = default, string? IssueKey = null, string? EpicKey = null);

/// <summary>An export column the user sets once per epic: its header, its short name, and where its value is kept.</summary>
/// <param name="Label">The column's header in the export, e.g. "Fase (ydelsesområde)".</param>
/// <param name="Name">The short name the values form uses for its field, e.g. "Fase".</param>
public record EpicColumn(string Label, string Name, Func<EpicExportValues, string?> Get, Action<EpicExportValues, string?> Set);

/// <summary>What a column's cells hold, so the table can align them: a date, hours, the epic link or plain text.</summary>
public enum ExportColumnKind { Text, Date, Hours, EpicLink }

/// <summary>One column of the export: its header and what its cells hold.</summary>
public record ExportColumn(string Label, ExportColumnKind Kind);

/// <summary>
/// A calendar month of logged time in the agency's tab-separated format: every day of the month, one row
/// per day and issue, blank apart from the date on days with no time.
/// </summary>
public static class MonthExport
{
    /// <summary>The columns set per epic, in export order.</summary>
    public static readonly IReadOnlyList<EpicColumn> EpicColumns =
    [
        new("Fase (ydelsesområde)", "Fase", v => v.Fase, (v, text) => v.Fase = text),
        new("Forordning", "Forordning", v => v.Forordning, (v, text) => v.Forordning = text),
        new("Hovedleverance nr.", "Hovedleverance nr.", v => v.HovedleveranceNr, (v, text) => v.HovedleveranceNr = text),
        new("Hovedleverance link", "Hovedleverance link", v => v.HovedleveranceLink, (v, text) => v.HovedleveranceLink = text),
    ];

    /// <summary>All eight columns, in export order.</summary>
    public static readonly IReadOnlyList<ExportColumn> Columns =
    [
        new("Dato (yyyy-mm-dd)", ExportColumnKind.Date),
        new("Timer", ExportColumnKind.Hours),
        .. EpicColumns.Select(column => new ExportColumn(column.Label, ExportColumnKind.Text)),
        new("Epic", ExportColumnKind.EpicLink),
        new("Beskrivelse", ExportColumnKind.Text),
    ];

    /// <summary>The export's header line: the column labels.</summary>
    public static readonly IReadOnlyList<string> Header = Columns.Select(column => column.Label).ToList();

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

    /// <summary>Hours of all rows added up.</summary>
    public static TimeSpan Total(IEnumerable<ExportRow> rows) => rows.Select(r => r.Hours).Sum();

    /// <summary>Rows with an issue (not the date-only rows of days without time).</summary>
    public static int IssueRowCount(IEnumerable<ExportRow> rows) => rows.Count(r => r.IssueKey is not null);

    /// <summary>Issues in the rows that have no epic, each once, by key.</summary>
    public static IReadOnlyList<string> IssuesWithoutEpic(IEnumerable<ExportRow> rows)
    {
        var keys = rows.Where(r => r.IssueKey is not null && r.EpicKey is null).Select(r => r.IssueKey!);
        return keys.Distinct().Order(StringComparer.Ordinal).ToList();
    }

    /// <summary>The row's eight cells, in <see cref="Columns"/> order.</summary>
    /// <param name="values">Epic key → the user's values for it.</param>
    public static IReadOnlyList<string> Cells(ExportRow row, string baseUrl, IReadOnlyDictionary<string, EpicExportValues> values)
    {
        var date = row.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (row.IssueKey is null) return [date, .. Enumerable.Repeat("", Columns.Count - 1)];
        var epicValues = row.EpicKey is null ? null : values.GetValueOrDefault(row.EpicKey);
        return
        [
            date,
            Hours(row.Hours),
            .. EpicColumns.Select(column => epicValues is null ? "" : column.Get(epicValues) ?? ""),
            row.EpicKey is null ? "" : JiraIssue.Url(baseUrl, row.EpicKey),
            row.IssueKey,
        ];
    }

    /// <summary>Header and rows, tab-separated, one line each. Tabs and line breaks in values become spaces.</summary>
    public static string ToTsv(IEnumerable<ExportRow> rows, string baseUrl, IReadOnlyDictionary<string, EpicExportValues> values) =>
        string.Join("\r\n", rows.Select(r => Cells(r, baseUrl, values)).Prepend(Header).Select(Line)) + "\r\n";

    /// <summary>Hours with a decimal comma: 7,5 and 0,25.</summary>
    public static string Hours(TimeSpan hours) =>
        Math.Round(hours.TotalHours, 2).ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');

    private static string Line(IEnumerable<string> cells) =>
        string.Join('\t', cells.Select(c => c.Replace("\r\n", " ").Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ').Trim()));
}
