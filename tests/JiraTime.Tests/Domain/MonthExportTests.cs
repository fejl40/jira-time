using JiraTime.Domain;
using static JiraTime.Tests.Support.TestEntries;

namespace JiraTime.Tests.Domain;

public class MonthExportTests
{
    private const string BaseUrl = "https://jira.uim.dk";
    private static readonly DateOnly Feb = new(2026, 2, 10);

    private static DateOnly February(int day) => new(2026, 2, day);

    private static readonly Dictionary<string, string?> NoEpics = new();
    private static readonly Dictionary<string, EpicExportValues> NoValues = new();

    [Fact]
    public void Every_day_of_the_month_gets_a_row()
    {
        var rows = MonthExport.Rows(Feb, [], NoEpics);

        Assert.Equal(28, rows.Count);
        Assert.Equal(new DateOnly(2026, 2, 1), rows[0].Date); // a Sunday
        Assert.Equal(new DateOnly(2026, 2, 28), rows[^1].Date);
        Assert.All(rows, r => Assert.Null(r.IssueKey));
    }

    [Fact]
    public void A_day_without_time_has_only_the_date()
    {
        var tsv = MonthExport.ToTsv(MonthExport.Rows(Feb, [], NoEpics), BaseUrl, NoValues);

        Assert.Contains("\r\n2026-02-01\t\t\t\t\t\t\t\r\n", tsv);
    }

    [Fact]
    public void Time_on_the_same_issue_and_day_is_added_up()
    {
        var rows = MonthExport.Rows(Feb, [Draft("BAC-578", 1.5, February(2)), Draft("BAC-577", 6, February(2)), Draft("bac-578", 0.5, February(2)), Draft("BAC-577", 7.5, February(3))], NoEpics);

        var feb2 = rows.Where(r => r.Date == new DateOnly(2026, 2, 2)).ToList();
        Assert.Equal(["BAC-577", "BAC-578"], feb2.Select(r => r.IssueKey));
        Assert.Equal(TimeSpan.FromHours(2), feb2[1].Hours);
        Assert.Equal(29, rows.Count); // one extra row for the second issue on the 2nd
    }

    [Fact]
    public void Time_outside_the_month_and_removed_time_is_left_out()
    {
        var removed = Draft("BAC-1", 2, February(4));
        removed.MarkRemovalPending();
        var march = Draft("BAC-1", 1, new DateOnly(2026, 3, 1));

        Assert.All(MonthExport.Rows(Feb, [removed, march], NoEpics), r => Assert.Null(r.IssueKey));
    }

    [Theory]
    [InlineData(8, "8")]
    [InlineData(7.5, "7.5")]
    [InlineData(0.25, "0.25")]
    [InlineData(1.0 / 3, "0.33")]
    public void Hours_have_a_decimal_point(double hours, string text) =>
        Assert.Equal(text, MonthExport.Hours(TimeSpan.FromHours(hours)));

    [Fact]
    public void Hours_have_a_decimal_point_on_a_danish_machine_too()
    {
        var before = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("da-DK");
        try
        {
            Assert.Equal("7.5", MonthExport.Hours(TimeSpan.FromHours(7.5)));
            Assert.Equal("7.5", JiraTime.Components.Format.Hours(TimeSpan.FromHours(7.5)));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = before;
        }
    }

    [Fact]
    public void An_issue_with_an_epic_gets_the_epic_link_and_the_epics_values()
    {
        var epics = new Dictionary<string, string?> { ["BAC-577"] = "BAC-500" };
        var values = new Dictionary<string, EpicExportValues>(StringComparer.OrdinalIgnoreCase)
        {
            ["BAC-500"] = new() { EpicKey = "BAC-500", Fase = "13. Tilpasning af eksisterende it-systemer", Forordning = "Schengen SIS / 76811", HovedleveranceNr = "4", HovedleveranceLink = "https://x/4" },
        };
        var row = MonthExport.Rows(Feb, [Draft("BAC-577", 8, February(2))], epics).Single(r => r.IssueKey is not null);

        Assert.Equal(
            ["2026-02-02", "8", "13. Tilpasning af eksisterende it-systemer", "Schengen SIS / 76811", "4", "https://x/4", "https://jira.uim.dk/browse/BAC-500", "BAC-577"],
            MonthExport.Cells(row, BaseUrl + "/", values));
    }

    [Fact]
    public void An_issue_without_an_epic_leaves_the_epic_columns_empty()
    {
        var row = MonthExport.Rows(Feb, [Draft("BAC-577", 8, February(2))], NoEpics).Single(r => r.IssueKey is not null);

        Assert.Equal(["2026-02-02", "8", "", "", "", "", "", "BAC-577"], MonthExport.Cells(row, BaseUrl, NoValues));
    }

    [Fact]
    public void The_text_starts_with_the_header_and_keeps_values_on_one_line()
    {
        var epics = new Dictionary<string, string?> { ["BAC-577"] = "BAC-500" };
        var values = new Dictionary<string, EpicExportValues> { ["BAC-500"] = new() { Fase = "a\tb\nc" } };

        var lines = MonthExport.ToTsv(MonthExport.Rows(Feb, [Draft("BAC-577", 8, February(2))], epics), BaseUrl, values).Split("\r\n");

        Assert.Equal("Dato (yyyy-mm-dd)\tTimer\tFase (ydelsesområde)\tForordning\tHovedleverance nr.\tHovedleverance link\tEpic\tBeskrivelse", lines[0]);
        Assert.Equal("2026-02-02\t8\ta b c\t\t\t\thttps://jira.uim.dk/browse/BAC-500\tBAC-577", lines[2]);
        Assert.Equal(28 + 2, lines.Length); // header, 28 days, and the trailing line break
    }

    [Fact]
    public void The_epic_columns_are_the_header_columns_after_the_date_and_hours()
    {
        Assert.Equal(MonthExport.Header.Skip(2).Take(4), MonthExport.EpicColumns.Select(c => c.Label));
        Assert.Equal(MonthExport.Header, MonthExport.Columns.Select(c => c.Label));
    }

    [Fact]
    public void Each_epic_column_reads_back_what_it_sets_and_leaves_the_others_alone()
    {
        foreach (var column in MonthExport.EpicColumns)
        {
            var values = new EpicExportValues();
            column.Set(values, "x " + column.Name);

            Assert.Equal("x " + column.Name, column.Get(values));
            Assert.Single(MonthExport.EpicColumns, c => c.Get(values) is not null);
            Assert.False(values.IsEmpty);
        }
    }

    [Fact]
    public void Values_without_text_in_any_epic_column_are_empty() =>
        Assert.True(new EpicExportValues { Fase = " ", Forordning = "" }.IsEmpty);

    [Fact]
    public void Totals_count_hours_issue_rows_and_issues_without_an_epic()
    {
        ExportRow[] rows =
        [
            new(new DateOnly(2026, 2, 1)),
            new(new DateOnly(2026, 2, 2), TimeSpan.FromHours(5), "BAC-2"),
            new(new DateOnly(2026, 2, 2), TimeSpan.FromHours(3), "BAC-1", "BAC-500"),
            new(new DateOnly(2026, 2, 3), TimeSpan.FromHours(1), "BAC-2"),
        ];

        Assert.Equal(TimeSpan.FromHours(9), MonthExport.Total(rows));
        Assert.Equal(3, MonthExport.IssueRowCount(rows));
        Assert.Equal(["BAC-2"], MonthExport.IssuesWithoutEpic(rows));
    }

    [Theory]
    [InlineData("https://jira.uim.dk")]
    [InlineData("https://jira.uim.dk/")]
    public void Issue_links_point_at_the_issue_page_in_Jira(string baseUrl) =>
        Assert.Equal("https://jira.uim.dk/browse/BAC-500", JiraIssue.Url(baseUrl, "BAC-500"));
}
