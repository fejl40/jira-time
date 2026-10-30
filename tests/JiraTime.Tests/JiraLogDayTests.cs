using JiraTime.Domain;
using JiraTime.Services;

namespace JiraTime.Tests;

/// <summary>A day of the Jira log as the Logs page shows it: grouped by action, filtered, updated live.</summary>
public class JiraLogDayTests
{
    private static readonly DateOnly Day = new(2026, 10, 30);
    private static readonly DateTimeOffset Nine = new(2026, 10, 30, 9, 0, 0, TimeSpan.Zero);

    private static JiraLogEntry Entry(string id, int minute, string? action = null, int? status = 200, string? body = null) => new()
    {
        Id = id, Time = Nine.AddMinutes(minute), Action = action ?? JiraLogEntry.Background, ActionId = action is null ? null : "id-" + action,
        Method = "GET", Url = $"https://jira.example.com/rest/api/2/{id}", Status = status, DurationMs = status is null ? null : 50, ResponseBody = body,
    };

    [Fact]
    public void Requests_are_grouped_by_action_newest_first()
    {
        var day = new JiraLogDay(Day, [
            Entry("a1", 0, "Open period"), Entry("b1", 1, "Save day"), Entry("a2", 2, "Open period"), Entry("b2", 3, "Save day")]);

        var groups = day.Groups(new());

        Assert.Equal(["Save day", "Open period"], groups.Select(g => g.Action));
        Assert.Equal(["b2", "b1"], groups[0].Requests.Select(r => r.Id));
        Assert.Equal(["a2", "a1"], groups[1].Requests.Select(r => r.Id));
    }

    [Fact]
    public void Background_requests_next_to_each_other_share_a_group()
    {
        var day = new JiraLogDay(Day, [Entry("x1", 0), Entry("x2", 1), Entry("s", 2, "Sync now"), Entry("x3", 3)]);

        var groups = day.Groups(new());

        Assert.Equal(["Background", "Sync now", "Background"], groups.Select(g => g.Action));
        Assert.Equal(["x2", "x1"], groups[2].Requests.Select(r => r.Id));
    }

    [Fact]
    public void Errors_only_and_text_filter_the_requests()
    {
        var day = new JiraLogDay(Day, [Entry("ok", 0, body: """{"key":"ABC-7"}"""), Entry("bad", 1, status: 500), Entry("down", 2, status: null) with { Error = "Could not reach", DurationMs = 10 }]);

        Assert.Equal(["down", "bad"], day.Groups(new(ErrorsOnly: true)).SelectMany(g => g.Requests).Select(r => r.Id));
        Assert.Equal(["ok"], day.Groups(new(Text: "abc-7")).SelectMany(g => g.Requests).Select(r => r.Id));
        Assert.Equal(["bad"], day.Groups(new(Text: "/bad")).SelectMany(g => g.Requests).Select(r => r.Id));
    }

    [Fact]
    public void A_live_request_appears_pending_and_updates_when_it_finishes()
    {
        var day = new JiraLogDay(Day, []);
        var started = Entry("r", 0, "Save all", status: null);

        day.Update(started);
        Assert.True(day.Groups(new()).Single().Pending);
        day.Update(started with { Status = 201, DurationMs = 80 });
        day.Update(started); // a late start changes nothing

        var request = day.Groups(new()).Single().Requests.Single();
        Assert.Equal(201, request.Status);
        Assert.Equal(1, day.Count);
    }
}

/// <summary>The Logs page reads, hears of and clears only the signed-in user's requests.</summary>
public class MyJiraLogTests : IDisposable
{
    private readonly TimeHarness _h = new();

    public void Dispose() => _h.Dispose();

    private MyJiraLog For(UserKey key) => new(_h.RequestLog, _h.Feed, CurrentUser.SignedIn(key), _h.Clock);

    [Fact]
    public async Task Users_only_ever_see_their_own_requests()
    {
        await _h.Log.GetRangeAsync(_h.Today, _h.Today);
        await _h.RequestLog.AppendAsync(new JiraLogEntry { Id = "annas", Owner = TestUsers.Anna.Id, Time = _h.Clock.Now });

        var carls = await For(TestUsers.Carl).ReadDayAsync(_h.Today);
        var annas = await For(TestUsers.Anna).ReadDayAsync(_h.Today);

        Assert.Equal(1, carls.Count);
        Assert.DoesNotContain(carls.Groups(new()).SelectMany(g => g.Requests), r => r.Id == "annas");
        Assert.Equal("annas", annas.Groups(new()).Single().Requests.Single().Id);
        Assert.DoesNotContain("annas", await For(TestUsers.Carl).ReadDayTextAsync(_h.Today));
    }

    [Fact]
    public async Task Live_requests_reach_only_their_user()
    {
        var annas = new List<JiraRequestEvent>();
        var carls = new List<JiraRequestEvent>();
        using var a = For(TestUsers.Anna).Subscribe(e => { lock (annas) annas.Add(e); return Task.CompletedTask; });
        using var c = For(TestUsers.Carl).Subscribe(e => { lock (carls) carls.Add(e); return Task.CompletedTask; });

        await _h.Log.GetRangeAsync(_h.Today, _h.Today);

        for (var i = 0; i < 200 && carls.Count < 2; i++) await Task.Delay(10);
        Assert.Equal(2, carls.Count);
        Assert.Empty(annas);
    }

    [Fact]
    public async Task Clearing_leaves_other_users_logs_and_today_stays_listed()
    {
        await _h.Log.GetRangeAsync(_h.Today, _h.Today);
        await _h.RequestLog.AppendAsync(new JiraLogEntry { Id = "annas", Owner = TestUsers.Anna.Id, Time = _h.Clock.Now });

        await For(TestUsers.Carl).ClearAsync();

        Assert.Equal(0, (await For(TestUsers.Carl).ReadDayAsync(_h.Today)).Count);
        Assert.Equal(1, (await For(TestUsers.Anna).ReadDayAsync(_h.Today)).Count);
        Assert.Equal([_h.Today], For(TestUsers.Carl).Days());
    }
}
