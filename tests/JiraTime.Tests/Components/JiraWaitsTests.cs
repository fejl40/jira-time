using JiraTime.Components;
using JiraTime.Domain;
using JiraTime.Components.Sheet;
using JiraTime.Services.Jira;

namespace JiraTime.Tests.Components;

public class JiraWaitsTests
{
    private static readonly Period October = Period.For(new DateOnly(2026, 10, 20));

    [Fact]
    public void Nothing_waits_at_first()
    {
        var waits = new JiraWaits();

        Assert.True(waits.CanStart);
        Assert.False(waits.PeriodPending);
        Assert.False(waits.DayPending);
        Assert.Null(waits.Status(October));
    }

    [Fact]
    public async Task A_running_sync_turns_the_buttons_off_and_marks_the_period_pending()
    {
        var waits = new JiraWaits();
        var jira = new TaskCompletionSource();

        var sync = waits.RunAsync(JiraWait.Sync, () => jira.Task);

        Assert.False(waits.CanStart);
        Assert.True(waits.PeriodPending);
        Assert.True(waits.DayPending);
        Assert.Equal("Syncing with Jira…", waits.Status(October));

        jira.SetResult();
        await sync;
        Assert.True(waits.CanStart);
        Assert.Null(waits.Status(October));
    }

    [Fact]
    public async Task A_failed_request_stops_waiting()
    {
        var waits = new JiraWaits();

        await Assert.ThrowsAsync<JiraException>(() => waits.RunAsync(JiraWait.SaveAll, () => throw new JiraException("down")));

        Assert.True(waits.CanStart);
    }

    [Fact]
    public async Task Every_start_and_end_is_announced()
    {
        var waits = new JiraWaits();
        var seen = new List<bool>();
        waits.Changed += () => seen.Add(waits.CanStart);

        await waits.RunAsync(JiraWait.Period, () => Task.CompletedTask);

        Assert.Equal([false, true], seen);
    }

    [Fact]
    public async Task Opening_a_period_shows_the_old_one_pending_until_the_new_one_is_read()
    {
        var waits = new JiraWaits();
        var november = October.Next;
        var jira = new TaskCompletionSource<string>();

        var open = waits.OpenAsync(november, () => jira.Task);

        Assert.True(waits.PeriodPending);
        Assert.Equal($"Opening {Format.Range(november.Start, november.End)}…", waits.Status(October));

        jira.SetResult("read");
        Assert.Equal("read", await open);
        Assert.False(waits.PeriodPending);
    }

    [Fact]
    public async Task A_period_left_before_it_was_read_is_not_shown()
    {
        var waits = new JiraWaits();
        var first = new TaskCompletionSource<string>();
        var second = new TaskCompletionSource<string>();

        var openFirst = waits.OpenAsync(October.Next, () => first.Task);
        var openSecond = waits.OpenAsync(October.Next.Next, () => second.Task);
        second.SetResult("second");
        first.SetResult("first");

        Assert.Equal("second", await openSecond);
        Assert.Null(await openFirst);
        Assert.True(waits.CanStart);
    }

    [Fact]
    public async Task Time_off_says_so_while_it_reads_the_period_again()
    {
        var waits = new JiraWaits();
        var jira = new TaskCompletionSource();

        var timeOff = waits.RunAsync(JiraWait.TimeOff, () => waits.RunAsync(JiraWait.Period, () => jira.Task));

        Assert.False(waits.CanStart);
        Assert.True(waits.PeriodPending);
        Assert.True(waits.DayPending);
        Assert.Equal("Saving time off…", waits.Status(October));

        jira.SetResult();
        await timeOff;
        Assert.True(waits.CanStart);
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void Aria_busy_is_written_out(bool busy, string value) => Assert.Equal(value, JiraWaits.Aria(busy));
}
