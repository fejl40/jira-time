using System.Net;
using System.Text;
using System.Text.Json;
using JiraTime.Domain;
using JiraTime.Services;

namespace JiraTime.Tests;

/// <summary>Every Jira request written to the user's JSONL log for the day, without credentials.</summary>
public class JiraRequestLogTests : IDisposable
{
    private readonly TimeHarness _h = new();
    private DateOnly Day => _h.Today;

    public void Dispose() => _h.Dispose();

    private Task<IReadOnlyList<JiraLogEntry>> CarlsLogAsync() => _h.RequestLog.ReadDayAsync(TestUsers.Carl.Id, Day);

    [Fact]
    public async Task Saving_a_day_logs_each_request_with_what_was_sent_and_what_came_back()
    {
        await _h.SaveDayAsync(Day, [TestEntries.Row("ABC-1", 2, "Reviewed the plan")]);

        var log = await CarlsLogAsync();
        var post = Assert.Single(log, e => e.Method == "POST");
        Assert.Equal("https://jira.example.com/rest/api/2/issue/ABC-1/worklog?adjustEstimate=auto", post.Url);
        Assert.Equal("/rest/api/2/issue/ABC-1/worklog?adjustEstimate=auto", post.Path);
        Assert.Equal(201, post.Status);
        Assert.Equal("Created", post.Reason);
        Assert.NotNull(post.DurationMs);
        Assert.Contains("Reviewed the plan", post.RequestBody);
        Assert.Contains("\"id\"", post.ResponseBody);
        Assert.StartsWith("application/json", post.ContentType);
        Assert.Equal(Encoding.UTF8.GetByteCount(post.ResponseBody!), post.ResponseSize);
        Assert.Equal("application/json", post.RequestHeaders["Accept"]);
        Assert.Contains("Content-Type", post.ResponseHeaders.Keys);
        Assert.Equal(TestUsers.Carl.Id, post.Owner);
        Assert.Equal(JiraLogEntry.Background, post.Action);
        Assert.Null(post.Error);
        Assert.True(File.Exists(_h.RequestLog.FileFor(TestUsers.Carl.Id, Day)));
        Assert.Equal(_h.Jira.Requests.Count, log.Count);
    }

    [Fact]
    public async Task A_request_Jira_refuses_is_logged_with_its_status_and_the_error()
    {
        _h.Jira.FailWith = HttpStatusCode.InternalServerError;

        await Assert.ThrowsAsync<JiraException>(() => _h.MyselfAsync());

        var entry = Assert.Single(await CarlsLogAsync());
        Assert.Equal(500, entry.Status);
        Assert.Equal("Failed with 500", entry.Error);
        Assert.True(entry.Failed);
    }

    [Fact]
    public async Task Unreachable_Jira_is_logged_with_why_and_no_status()
    {
        _h.Jira.Offline = true;

        await Assert.ThrowsAsync<JiraException>(() => _h.MyselfAsync());

        var entry = Assert.Single(await CarlsLogAsync());
        Assert.Null(entry.Status);
        Assert.Contains("Could not reach jira.example.com", entry.Error);
    }

    [Fact]
    public async Task A_request_that_times_out_is_logged_as_timed_out()
    {
        var client = new JiraClient(_h.HttpOver(new StubHandler(_ => throw new TaskCanceledException())));

        await Assert.ThrowsAsync<JiraException>(() => client.GetMyselfAsync(_h.Connection));

        var entry = Assert.Single(await CarlsLogAsync());
        Assert.Equal("jira.example.com did not respond in time.", entry.Error);
    }

    [Fact]
    public async Task An_answer_that_is_not_JSON_is_logged_as_it_came()
    {
        var html = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>Log in</html>", Encoding.UTF8, "text/html") });
        var client = new JiraClient(_h.HttpOver(html));

        await Assert.ThrowsAsync<JiraException>(() => client.GetMyselfAsync(_h.Connection));

        var entry = Assert.Single(await CarlsLogAsync());
        Assert.Equal("<html>Log in</html>", entry.ResponseBody);
        Assert.StartsWith("text/html", entry.ContentType);
        Assert.Contains("did not return JSON", entry.Error);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task The_token_and_the_Authorization_header_never_reach_the_log(bool cloud)
    {
        const string token = "s3cret-token-4711";
        using var h = new TimeHarness((cloud ? TimeHarness.Cloud : TimeHarness.Server) with { Token = token });
        h.Jira.FailWith = HttpStatusCode.Unauthorized;
        await Assert.ThrowsAsync<JiraException>(() => h.MyselfAsync());
        h.Jira.FailWith = null;
        await h.SaveDayAsync(h.Today, [TestEntries.Row("ABC-1", 1)]);

        var text = string.Concat(Directory.EnumerateFiles(Path.Combine(h.Files.Path, "logs", "jira"), "*.jsonl", SearchOption.AllDirectories).Select(File.ReadAllText));
        Assert.NotEmpty(text);
        Assert.DoesNotContain(token, text);
        Assert.DoesNotContain(Convert.ToBase64String(Encoding.UTF8.GetBytes($"carl@example.com:{token}")), text);
        Assert.DoesNotContain("Authorization", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer", text);
    }

    [Fact]
    public async Task Large_bodies_are_cut_and_marked_truncated()
    {
        var big = new string('x', JiraRequestRecorder.MaxBody + 10);
        _h.Jira.Myself = new { key = "JIRAUSER1", displayName = big };

        await _h.MyselfAsync();
        await _h.SaveDayAsync(Day, [TestEntries.Row("ABC-1", 1, big)]);

        var log = await CarlsLogAsync();
        var myself = log.Single(e => e.Path.EndsWith("/myself"));
        Assert.True(myself.ResponseBodyTruncated);
        Assert.Equal(JiraRequestRecorder.MaxBody, myself.ResponseBody!.Length);
        Assert.True(myself.ResponseSize > JiraRequestRecorder.MaxBody);
        var post = log.Single(e => e.Method == "POST");
        Assert.True(post.RequestBodyTruncated);
        Assert.False(post.ResponseBodyTruncated);
    }

    [Fact]
    public async Task Parallel_requests_write_whole_lines()
    {
        var writes = Enumerable.Range(0, 60).Select(i => _h.RequestLog.AppendAsync(
            new JiraLogEntry { Id = $"r{i}", Owner = TestUsers.Carl.Id, Time = _h.Clock.Now, ResponseBody = new string('y', 5000) }));
        await Task.WhenAll(writes);

        var log = await CarlsLogAsync();
        Assert.Equal(60, log.Count);
        Assert.Equal(60, log.Select(e => e.Id).Distinct().Count());
    }

    [Fact]
    public async Task Files_older_than_30_days_are_deleted()
    {
        var old = _h.RequestLog.FileFor(TestUsers.Anna.Id, Day.AddDays(-31));
        var recent = _h.RequestLog.FileFor(TestUsers.Anna.Id, Day.AddDays(-29));
        Directory.CreateDirectory(Path.GetDirectoryName(old)!);
        File.WriteAllText(old, "{}\n");
        File.WriteAllText(recent, "{}\n");

        await _h.Log.GetRangeAsync(Day, Day);

        Assert.False(File.Exists(old));
        Assert.True(File.Exists(recent));
        Assert.Equal([Day.AddDays(-29)], _h.RequestLog.Days(TestUsers.Anna.Id));
    }

    [Fact]
    public async Task Clearing_deletes_only_the_users_own_logs()
    {
        await _h.Log.GetRangeAsync(Day, Day);
        await _h.RequestLog.AppendAsync(new JiraLogEntry { Id = "a", Owner = TestUsers.Anna.Id, Time = _h.Clock.Now });

        await _h.RequestLog.ClearAsync(TestUsers.Carl.Id);

        Assert.Empty(_h.RequestLog.Days(TestUsers.Carl.Id));
        Assert.Single(await _h.RequestLog.ReadDayAsync(TestUsers.Anna.Id, Day));
    }

    [Fact]
    public async Task A_new_log_file_starts_with_the_first_line_not_a_byte_order_mark()
    {
        await _h.Log.GetRangeAsync(Day, Day);

        var bytes = await File.ReadAllBytesAsync(_h.RequestLog.FileFor(TestUsers.Carl.Id, Day));
        Assert.Equal((byte)'{', bytes[0]);
    }

    [Fact]
    public async Task A_log_file_written_with_a_byte_order_mark_still_reads()
    {
        var entry = new JiraLogEntry { Id = "old", Owner = TestUsers.Anna.Id, Time = _h.Clock.Now };
        var file = _h.RequestLog.FileFor(TestUsers.Anna.Id, Day);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        var line = JsonSerializer.Serialize(entry, new JsonSerializerOptions(JsonSerializerDefaults.Web)) + "\n";
        await File.WriteAllTextAsync(file, line, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        Assert.Equal("old", Assert.Single(await _h.RequestLog.ReadDayAsync(TestUsers.Anna.Id, Day)).Id);
        Assert.StartsWith("{", await _h.RequestLog.ReadDayTextAsync(TestUsers.Anna.Id, Day));
    }

    [Fact]
    public void An_owner_that_is_not_an_id_is_refused() =>
        Assert.Throws<ArgumentException>(() => _h.RequestLog.FileFor("../keys", Day));
}

/// <summary>An HTTP handler that answers every request with <paramref name="answer"/>.</summary>
public sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => Task.FromResult(answer(request));
}
