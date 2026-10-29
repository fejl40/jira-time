using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Storage;

namespace JiraTime.Tests;

public class MonthExportTests
{
    private const string BaseUrl = "https://jira.uim.dk";
    private static readonly DateOnly Feb = new(2026, 2, 10);

    private static TimeEntry Entry(string key, int day, double hours) =>
        new() { IssueKey = key, Date = new DateOnly(2026, 2, day), Duration = TimeSpan.FromHours(hours) };

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
        var rows = MonthExport.Rows(Feb, [Entry("BAC-578", 2, 1.5), Entry("BAC-577", 2, 6), Entry("bac-578", 2, 0.5), Entry("BAC-577", 3, 7.5)], NoEpics);

        var feb2 = rows.Where(r => r.Date == new DateOnly(2026, 2, 2)).ToList();
        Assert.Equal(["BAC-577", "BAC-578"], feb2.Select(r => r.IssueKey));
        Assert.Equal(TimeSpan.FromHours(2), feb2[1].Hours);
        Assert.Equal(29, rows.Count); // one extra row for the second issue on the 2nd
    }

    [Fact]
    public void Time_outside_the_month_and_removed_time_is_left_out()
    {
        var removed = Entry("BAC-1", 4, 2);
        removed.DeletePending = true;
        var march = new TimeEntry { IssueKey = "BAC-1", Date = new DateOnly(2026, 3, 1), Duration = TimeSpan.FromHours(1) };

        Assert.All(MonthExport.Rows(Feb, [removed, march], NoEpics), r => Assert.Null(r.IssueKey));
    }

    [Theory]
    [InlineData(8, "8")]
    [InlineData(7.5, "7,5")]
    [InlineData(0.25, "0,25")]
    [InlineData(1.0 / 3, "0,33")]
    public void Hours_have_a_decimal_comma(double hours, string text) =>
        Assert.Equal(text, MonthExport.Hours(TimeSpan.FromHours(hours)));

    [Fact]
    public void An_issue_with_an_epic_gets_the_epic_link_and_the_epics_values()
    {
        var epics = new Dictionary<string, string?> { ["BAC-577"] = "BAC-500" };
        var values = new Dictionary<string, EpicExportValues>(StringComparer.OrdinalIgnoreCase)
        {
            ["BAC-500"] = new() { EpicKey = "BAC-500", Fase = "13. Tilpasning af eksisterende it-systemer", Forordning = "Schengen SIS / 76811", HovedleveranceNr = "4", HovedleveranceLink = "https://x/4" },
        };
        var row = MonthExport.Rows(Feb, [Entry("BAC-577", 2, 8)], epics).Single(r => r.IssueKey is not null);

        Assert.Equal(
            ["2026-02-02", "8", "13. Tilpasning af eksisterende it-systemer", "Schengen SIS / 76811", "4", "https://x/4", "https://jira.uim.dk/browse/BAC-500", "BAC-577"],
            MonthExport.Cells(row, BaseUrl + "/", values));
    }

    [Fact]
    public void An_issue_without_an_epic_leaves_the_epic_columns_empty()
    {
        var row = MonthExport.Rows(Feb, [Entry("BAC-577", 2, 8)], NoEpics).Single(r => r.IssueKey is not null);

        Assert.Equal(["2026-02-02", "8", "", "", "", "", "", "BAC-577"], MonthExport.Cells(row, BaseUrl, NoValues));
    }

    [Fact]
    public void The_text_starts_with_the_header_and_keeps_values_on_one_line()
    {
        var epics = new Dictionary<string, string?> { ["BAC-577"] = "BAC-500" };
        var values = new Dictionary<string, EpicExportValues> { ["BAC-500"] = new() { Fase = "a\tb\nc" } };

        var lines = MonthExport.ToTsv(MonthExport.Rows(Feb, [Entry("BAC-577", 2, 8)], epics), BaseUrl, values).Split("\r\n");

        Assert.Equal("Dato (yyyy-mm-dd)\tTimer\tFase (ydelsesområde)\tForordning\tHovedleverance nr.\tHovedleverance link\tEpic\tBeskrivelse", lines[0]);
        Assert.Equal("2026-02-02\t8\ta b c\t\t\t\thttps://jira.uim.dk/browse/BAC-500\tBAC-577", lines[2]);
        Assert.Equal(28 + 2, lines.Length); // header, 28 days, and the trailing line break
    }
}

public class EpicExportStoreTests : IDisposable
{
    private readonly TempDir _dir = new();
    private readonly FileDocumentStore _store;

    public EpicExportStoreTests() => _store = new FileDocumentStore(_dir.Path);

    private EpicExportStore For(UserKey user) => new(_store, CurrentUser.SignedIn(user));

    [Fact]
    public async Task Values_are_saved_and_read_back_per_epic()
    {
        var carl = For(TestUsers.Carl);
        await carl.SaveAsync(new EpicExportValues { EpicKey = "bac-500", Fase = " 13. Tilpasning ", Forordning = "Schengen SIS / 76811", HovedleveranceNr = "", HovedleveranceLink = "https://x/4" });

        var v = (await carl.GetAsync(["BAC-500", "BAC-501"]))["BAC-500"];

        Assert.Equal("13. Tilpasning", v.Fase);
        Assert.Equal("Schengen SIS / 76811", v.Forordning);
        Assert.Null(v.HovedleveranceNr);
        Assert.Equal("https://x/4", v.HovedleveranceLink);
        Assert.Single(await carl.GetAsync(["BAC-500", "BAC-501"]));
    }

    [Fact]
    public async Task Each_user_has_their_own_values()
    {
        await For(TestUsers.Carl).SaveAsync(new EpicExportValues { EpicKey = "BAC-500", Fase = "Carl's" });

        Assert.Empty(await For(TestUsers.Anna).GetAsync(["BAC-500"]));

        await For(TestUsers.Anna).SaveAsync(new EpicExportValues { EpicKey = "BAC-500", Fase = "Anna's" });
        Assert.Equal("Carl's", (await For(TestUsers.Carl).GetAsync(["BAC-500"]))["BAC-500"].Fase);
    }

    [Fact]
    public async Task Clearing_every_value_removes_them()
    {
        var carl = For(TestUsers.Carl);
        await carl.SaveAsync(new EpicExportValues { EpicKey = "BAC-500", Fase = "x" });
        await carl.SaveAsync(new EpicExportValues { EpicKey = "BAC-500", Fase = " " });

        Assert.Empty(await carl.GetAsync(["BAC-500"]));
    }

    public void Dispose()
    {
        _store.Dispose();
        _dir.Dispose();
    }
}

public class ExportLinksTests
{
    [Fact]
    public void Links_name_the_month() =>
        Assert.Equal("export?month=2026-02", JiraTime.Components.ExportLinks.To(new DateOnly(2026, 2, 16)));

    [Theory]
    [InlineData("2026-02", "2026-02-01")]
    [InlineData(" 2026-12 ", "2026-12-01")]
    [InlineData("2026-13", null)]
    [InlineData("feb", null)]
    [InlineData(null, null)]
    public void Links_are_read_back(string? month, string? first) =>
        Assert.Equal(first is null ? null : DateOnly.Parse(first), JiraTime.Components.ExportLinks.Read(month));

    [Theory]
    [InlineData("https://jira.uim.dk")]
    [InlineData("https://jira.uim.dk/")]
    public void Issue_links_point_at_the_issue_page_in_Jira(string baseUrl) =>
        Assert.Equal("https://jira.uim.dk/browse/BAC-500", JiraIssue.Url(baseUrl, "BAC-500"));
}
