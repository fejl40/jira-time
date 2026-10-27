using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Storage.FileSystem;

namespace JiraTime.Tests;

public class TimeEntryTests
{
    private static readonly DateOnly Day = new(2026, 10, 19);

    private static TimeEntry Entry(string key, double hours) => new() { IssueKey = key, Date = Day, Duration = TimeSpan.FromHours(hours) };

    [Fact]
    public void A_valid_entry_has_no_error() => Assert.Null(TimeEntry.Validate(Entry("ABC-1", 1.5)));

    [Fact]
    public void An_issue_is_needed() => Assert.NotNull(TimeEntry.Validate(Entry(" ", 1)));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Hours_must_be_positive(double hours) => Assert.NotNull(TimeEntry.Validate(Entry("ABC-1", hours)));

    [Fact]
    public void Hours_must_be_whole_minutes() =>
        Assert.NotNull(TimeEntry.Validate(new TimeEntry { IssueKey = "ABC-1", Date = Day, Duration = TimeSpan.FromSeconds(90) }));

    [Fact]
    public void One_entry_has_at_most_24_hours()
    {
        Assert.Null(TimeEntry.Validate(Entry("ABC-1", 24)));
        Assert.Equal(TimeEntry.TooManyHours, TimeEntry.Validate(Entry("ABC-1", 24.5)));
    }
}

/// <summary>The retry queue as stored: per user, read back after a restart, old formats still readable.</summary>
public class RetryQueueStoreTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 19);

    private readonly TempDir _dir = new();
    private readonly FileDocumentStore _store;

    public RetryQueueStoreTests() => _store = new FileDocumentStore(new FileDocumentStoreOptions { RootPath = _dir.Path });

    public void Dispose()
    {
        _store.Dispose();
        _dir.Dispose();
    }

    private RetryQueue Queue(UserKey? user = null) => new(_store, TestUsers.SignedIn(user));

    private static TimeEntry Waiting(string key, DateOnly? day = null) =>
        new() { IssueKey = key, Date = day ?? Day, Duration = TimeSpan.FromHours(1), SyncPending = true };

    [Fact]
    public async Task Waiting_changes_in_the_range_come_in_day_order()
    {
        await Queue().AddAsync(Waiting("B", Day.AddDays(1)));
        await Queue().AddAsync(Waiting("A"));
        await Queue().AddAsync(Waiting("X", Day.AddDays(14)));

        Assert.Equal(["A", "B"], (await Queue().GetPendingAsync(Day, Day.AddDays(13))).Select(e => e.IssueKey));
    }

    [Fact]
    public async Task Adding_a_change_with_the_same_id_replaces_it_and_removing_takes_it_out()
    {
        var change = Waiting("ABC-1");
        await Queue().AddAsync(change);
        change.Duration = TimeSpan.FromHours(3);
        await Queue().AddAsync(change);

        Assert.Equal(TimeSpan.FromHours(3), Assert.Single(await Queue().GetPendingAsync(Day, Day)).Duration);
        await Queue().RemoveAsync(change.Id);
        Assert.Empty(await Queue().GetPendingAsync(Day, Day));
    }

    [Fact]
    public async Task Entries_saved_with_start_and_end_times_are_read_as_their_duration()
    {
        var folder = Path.Combine(_dir.Path, "time-entries");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "old.json"), $$"""
            { "id": "old", "owner": "{{TestUsers.Carl.Id}}", "issueKey": "ABC-1", "issueSummary": "", "date": "2026-10-19",
              "start": "09:00:00", "end": "11:30:00", "syncPending": true }
            """);

        using var store = new FileDocumentStore(new FileDocumentStoreOptions { RootPath = _dir.Path });
        var queue = new RetryQueue(store, TestUsers.SignedIn());
        var old = Assert.Single(await queue.GetPendingAsync(Day, Day));
        Assert.Equal(TimeSpan.FromHours(2.5), old.Duration);

        await queue.AddAsync(old);
        var json = await File.ReadAllTextAsync(Path.Combine(folder, "old.json"));
        Assert.DoesNotContain("\"start\"", json);
        Assert.Contains("\"duration\"", json);
    }
}
