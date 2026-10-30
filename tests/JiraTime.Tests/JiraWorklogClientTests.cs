using System.Text.Json.Nodes;
using System.Web;
using JiraTime.Domain;
using JiraTime.Services;
using static JiraTime.Tests.TestEntries;

namespace JiraTime.Tests;

/// <summary>Reading and writing worklogs in Jira's format: embedded worklogs, pages, the worklog body.</summary>
public class JiraWorklogClientTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 19);

    private readonly TimeHarness _h = new();
    private readonly JiraWorklogClient _client;

    public JiraWorklogClientTests() => _client = new JiraWorklogClient(_h.Http, _h.Clock);
    public void Dispose() => _h.Dispose();

    private Task<IReadOnlyList<JiraWorklog>> MineAsync() =>
        _client.GetMyWorklogsAsync(_h.Connection, _h.User.Jira!.Profile, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

    private int WorklogReads(string key) => _h.Jira.WorklogReads.Count(r => r.Path.Contains($"/issue/{key}/worklog"));

    [Fact]
    public async Task Only_an_issue_with_more_worklogs_than_the_search_sends_is_read_on_its_own()
    {
        _h.Jira.EmbeddedWorklogLimit = 3;
        for (var day = 19; day <= 23; day++)
            _h.Jira.AddWorklog("ABC-1", $"2026-10-{day}T09:00:00.000+0200", 3600);
        _h.Jira.AddWorklog("ABC-2", "2026-10-26T09:00:00.000+0100", 3600);

        var worklogs = await MineAsync();

        Assert.Equal(6, worklogs.Count);
        Assert.Equal(1, WorklogReads("ABC-1"));
        Assert.Equal(0, WorklogReads("ABC-2"));
    }

    [Fact]
    public async Task An_issue_read_on_its_own_is_read_page_by_page()
    {
        _h.Jira.EmbeddedWorklogLimit = 0;
        var all = Enumerable.Range(1, 5).Select(i => new JsonObject
        {
            ["id"] = i.ToString(), ["started"] = $"2026-10-{18 + i}T09:00:00.000+0200", ["timeSpentSeconds"] = 3600,
            ["author"] = _h.Jira.DefaultAuthor.DeepClone(),
        }).ToList();
        _h.Jira.Worklogs["ABC-1"] = new() { ["1"] = all[0] }; // so the worklog search finds the issue
        _h.Jira.Gets["/rest/api/2/issue/ABC-1/worklog"] = uri =>
        {
            var startAt = int.Parse(HttpUtility.ParseQueryString(uri.Query)["startAt"]!);
            var page = all.Skip(startAt).Take(2).Select(w => (JsonNode)w.DeepClone()).ToArray();
            return new JsonObject { ["startAt"] = startAt, ["total"] = all.Count, ["worklogs"] = new JsonArray(page) };
        };

        var worklogs = await MineAsync();

        Assert.Equal(["1", "2", "3", "4", "5"], worklogs.Select(w => w.Id));
        Assert.Equal(3, WorklogReads("ABC-1"));
    }

    [Fact]
    public async Task Cloud_uses_api_v3_and_a_document_comment()
    {
        using var cloud = new TimeHarness(TimeHarness.Cloud);

        await cloud.SaveDayAsync(Day, [Row("ABC-1", 1, "Review")]);

        var post = Assert.Single(cloud.Jira.Writes);
        Assert.StartsWith("/rest/api/3/issue/ABC-1/worklog", post.Path);
        Assert.Equal("doc", (string?)post.Body!["comment"]!["type"]);
    }

    [Theory]
    [InlineData(2, 0, "2026-10-19T09:00:00.000+0200")]
    [InlineData(-5, -30, "2026-10-19T09:00:00.000-0530")]
    [InlineData(0, 0, "2026-10-19T09:00:00.000+0000")]
    public async Task Started_is_9_oclock_in_the_servers_time_zone_with_an_offset_without_a_colon(int hours, int minutes, string expected)
    {
        var offset = new TimeSpan(hours, minutes, 0);
        _h.Clock.Zone = TimeZoneInfo.CreateCustomTimeZone("Test", offset, "Test", "Test");

        await _client.AddWorklogAsync(_h.Connection, Draft("ABC-1", 1, Day));

        var post = Assert.Single(_h.Jira.Writes);
        Assert.Equal(expected, (string?)post.Body!["started"]);
    }

    [Theory]
    [InlineData(3600, 60)]
    [InlineData(5400, 90)]
    [InlineData(89, 1)]
    [InlineData(91, 2)]
    [InlineData(10, 1)]
    public async Task Worklog_time_is_read_in_whole_minutes(int seconds, int minutes)
    {
        var id = _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", seconds);

        var worklogs = await _client.GetWorklogsOnAsync(_h.Connection, ["ABC-1"]);

        Assert.Equal(TimeSpan.FromMinutes(minutes), worklogs[id].Duration);
    }

    [Theory]
    [InlineData("2026-10-19T09:30:00.000+0200", "2026-10-19T07:30:00Z")]
    [InlineData("2026-10-19T09:30:00.000-0500", "2026-10-19T14:30:00Z")]
    [InlineData("2026-10-19T09:30:00.000+02:00", "2026-10-19T07:30:00Z")]
    [InlineData("nonsense", null)]
    [InlineData(null, null)]
    public async Task Created_times_are_read_with_or_without_a_colon_in_the_offset(string? text, string? utc)
    {
        var id = _h.Jira.AddWorklog("ABC-1", "2026-10-19T09:00:00.000+0200", 3600);
        _h.Jira.Worklogs["ABC-1"][id]["created"] = text;

        var worklogs = await _client.GetWorklogsOnAsync(_h.Connection, ["ABC-1"]);

        Assert.Equal(utc is null ? null : DateTimeOffset.Parse(utc), worklogs[id].Created);
    }
}
