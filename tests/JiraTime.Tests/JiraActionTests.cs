using System.Net;
using JiraTime.Domain;
using JiraTime.Services;

namespace JiraTime.Tests;

/// <summary>Each logged Jira request names the user action it was made for, and never another one.</summary>
public class JiraActionTests : IDisposable
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);
    private readonly TimeHarness _h = new();

    public void Dispose() => _h.Dispose();

    private Task<IReadOnlyList<JiraLogEntry>> LogOfAsync(UserKey user) => _h.RequestLog.ReadDayAsync(user.Id, _h.Today);

    private static async Task<T> WaitFor<T>(Func<Task<T>> read, Func<T, bool> done)
    {
        var until = DateTime.UtcNow + Wait;
        var value = await read();
        while (!done(value) && DateTime.UtcNow < until)
        {
            await Task.Delay(10);
            value = await read();
        }
        return value;
    }

    [Fact]
    public async Task Requests_of_one_action_share_its_name_and_id()
    {
        _h.Jira.AddWorklog("ABC-1", "2026-10-27T09:00:00.000+0000", 3600);

        using (JiraAction.Start("Save day Tue 27 Oct"))
            await _h.SaveDayAsync(_h.Today, [TimeHarness.Row("ABC-2", 1)]);

        var log = await LogOfAsync(TestUsers.Carl);
        Assert.True(log.Count > 2);
        Assert.All(log, e => Assert.Equal("Save day Tue 27 Oct", e.Action));
        Assert.Single(log.Select(e => e.ActionId).Distinct());
        Assert.NotNull(log[0].ActionId);
    }

    [Fact]
    public async Task An_issue_search_is_logged_as_its_own_action()
    {
        _h.Jira.Gets["/rest/api/2/issue/picker"] = _ => new { sections = new[] { new { issues = new[] { new { key = "ABC-7", summaryText = "Payroll" } } } } };
        var (_, issues) = _h.PeriodLoading();

        var found = await issues.SearchAsync("payroll");

        Assert.Equal("ABC-7", Assert.Single(found).Key);
        var entry = Assert.Single(await LogOfAsync(TestUsers.Carl));
        Assert.Equal("Search issues “payroll”", entry.Action);
    }

    [Fact]
    public async Task Searching_or_saving_without_a_signed_in_user_asks_nothing_of_Jira()
    {
        _h.User.Clear();
        var (_, issues) = _h.PeriodLoading();

        await Assert.ThrowsAsync<JiraException>(() => issues.SearchAsync("payroll"));
        await Assert.ThrowsAsync<JiraException>(() => _h.Sync.SyncRangeAsync(_h.Today, _h.Today));
        Assert.Empty(_h.Jira.Requests);
    }

    [Fact]
    public async Task Requests_outside_any_action_are_background()
    {
        using (JiraAction.Start("Sync now")) { }

        await _h.Log.GetRangeAsync(_h.Today, _h.Today);

        var entry = Assert.Single(await LogOfAsync(TestUsers.Carl));
        Assert.Equal(JiraLogEntry.Background, entry.Action);
        Assert.Null(entry.ActionId);
        Assert.Null(JiraAction.Current);
    }

    [Fact]
    public async Task A_start_inside_an_action_joins_it()
    {
        using var outer = JiraAction.Start("Save all");
        var first = JiraAction.Current;

        using (JiraAction.Start("Open period"))
            Assert.Same(first, JiraAction.Current);

        Assert.Same(first, JiraAction.Current);
        Assert.Equal("Save all", first!.Name);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task The_action_flows_through_awaits_and_tasks_started_inside_it()
    {
        using (JiraAction.Start("Open export October 2026"))
        {
            await Task.Yield();
            await Task.Run(() => _h.Log.GetRangeAsync(_h.Today, _h.Today));
        }

        Assert.Equal("Open export October 2026", Assert.Single(await LogOfAsync(TestUsers.Carl)).Action);
    }

    [Fact]
    public async Task Parallel_actions_in_other_tabs_never_get_each_others_action()
    {
        using var second = new TimeHarness();
        using var third = new TimeHarness(TimeHarness.Server with { Token = "anna" });
        var allOpen = new TaskCompletionSource();
        async Task RunAsync(TimeHarness h, string name)
        {
            using var _ = JiraAction.Start(name);
            await allOpen.Task; // every action is open before any reaches Jira
            for (var i = 0; i < 5; i++)
            {
                h.Log.Refresh();
                await h.Log.GetRangeAsync(h.Today, h.Today);
            }
        }

        var runs = new[] { (_h, "Sync now"), (second, "Save all"), (third, "Open period") }
            .Select(run => Task.Run(() => RunAsync(run.Item1, run.Item2))).ToList();
        allOpen.SetResult();
        await Task.WhenAll(runs);

        foreach (var (h, name) in new[] { (_h, "Sync now"), (second, "Save all"), (third, "Open period") })
        {
            var log = await h.RequestLog.ReadDayAsync(TestUsers.Carl.Id, h.Today);
            Assert.Equal(5, log.Count);
            Assert.All(log, e => Assert.Equal(name, e.Action));
            Assert.Single(log.Select(e => e.ActionId).Distinct());
        }
    }

    [Fact]
    public async Task Signing_in_logs_its_requests_under_the_user_Jira_confirms()
    {
        using (JiraAction.Start("Sign in"))
            await _h.Identity(new CurrentUser()).SignInAsync(TimeHarness.Server);

        var log = await WaitFor(() => LogOfAsync(TestUsers.Carl), l => l.Count > 0);
        var myself = Assert.Single(log);
        Assert.Equal("Sign in", myself.Action);
        Assert.EndsWith("/myself", myself.Path);
        Assert.Equal(TestUsers.Carl.Id, myself.Owner);
    }

    [Fact]
    public async Task A_sign_in_Jira_refuses_is_logged_as_signed_out()
    {
        _h.Jira.FailWith = HttpStatusCode.Unauthorized;

        using (JiraAction.Start("Connect to Jira"))
            await _h.Identity(new CurrentUser()).SignInAsync(TimeHarness.Server);

        var log = await WaitFor(() => _h.RequestLog.ReadDayAsync(JiraRequestLog.SignedOut, _h.Today), l => l.Count > 0);
        Assert.Equal(401, Assert.Single(log).Status);
        Assert.Empty(await LogOfAsync(TestUsers.Carl));
    }
}
