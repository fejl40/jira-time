using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Storage.FileSystem;

namespace JiraTime.Tests;

public class TimeEntryServiceTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 19);

    private readonly TempDir _dir = new();
    private readonly FileDocumentStore _store;
    private readonly TimeEntryService _service;

    public TimeEntryServiceTests()
    {
        _store = new FileDocumentStore(new FileDocumentStoreOptions { RootPath = _dir.Path });
        _service = new TimeEntryService(_store);
    }

    public void Dispose()
    {
        _store.Dispose();
        _dir.Dispose();
    }

    private static TimeEntry Entry(string key, double hours, DateOnly? date = null) =>
        new() { IssueKey = key, Date = date ?? Day, Duration = TimeSpan.FromHours(hours) };

    [Fact]
    public async Task Saves_a_valid_entry()
    {
        Assert.Null(await _service.SaveAsync(Entry("ABC-1", 1.5)));

        var entries = await _service.GetRangeAsync(Day, Day);
        Assert.Single(entries);
        Assert.Equal(TimeSpan.FromMinutes(90), entries[0].Duration);
    }

    [Fact]
    public async Task Rejects_a_missing_issue()
    {
        var error = await _service.SaveAsync(Entry(" ", 1));

        Assert.NotNull(error);
        Assert.Empty(await _service.GetRangeAsync(Day, Day));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Rejects_hours_that_are_not_positive(double hours)
    {
        Assert.NotNull(await _service.SaveAsync(Entry("ABC-1", hours)));
    }

    [Fact]
    public async Task Rejects_parts_of_a_minute()
    {
        Assert.NotNull(await _service.SaveAsync(new TimeEntry { IssueKey = "ABC-1", Date = Day, Duration = TimeSpan.FromSeconds(90) }));
    }

    [Fact]
    public async Task Several_entries_on_the_same_day_and_issue_are_fine()
    {
        Assert.Null(await _service.SaveAsync(Entry("ABC-1", 4)));
        Assert.Null(await _service.SaveAsync(Entry("ABC-1", 2)));
        Assert.Null(await _service.SaveAsync(Entry("ABC-2", 2)));
        Assert.Equal(3, (await _service.GetRangeAsync(Day, Day)).Count);
    }

    [Fact]
    public async Task Rejects_more_than_24_hours_in_a_day()
    {
        await _service.SaveAsync(Entry("ABC-1", 20));

        Assert.NotNull(await _service.SaveAsync(Entry("ABC-2", 4.5)));
        Assert.Null(await _service.SaveAsync(Entry("ABC-2", 4)));
        Assert.Null(await _service.SaveAsync(Entry("ABC-3", 8, Day.AddDays(1))));
    }

    [Fact]
    public async Task Editing_an_entry_does_not_count_its_old_hours()
    {
        var entry = Entry("ABC-1", 20);
        await _service.SaveAsync(entry);

        entry.Duration = TimeSpan.FromHours(24);

        Assert.Null(await _service.SaveAsync(entry));
        Assert.Single(await _service.GetRangeAsync(Day, Day));
    }

    [Fact]
    public async Task GetRange_returns_entries_in_range_sorted_by_date_then_creation()
    {
        await _service.SaveAsync(Entry("A", 1));
        await _service.SaveAsync(Entry("C", 1));
        await _service.SaveAsync(Entry("D", 1, Day.AddDays(1)));
        await _service.SaveAsync(Entry("X", 1, Day.AddDays(14)));

        var entries = await _service.GetRangeAsync(Day, Day.AddDays(13));

        Assert.Equal(["A", "C", "D"], entries.Select(e => e.IssueKey));
    }

    [Fact]
    public async Task Entries_saved_with_start_and_end_times_are_read_as_their_duration()
    {
        var folder = Path.Combine(_dir.Path, "time-entries");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "old.json"), """
            { "id": "old", "issueKey": "ABC-1", "issueSummary": "", "date": "2026-10-19",
              "start": "09:00:00", "end": "11:30:00", "syncPending": false, "jiraWorklogId": "1" }
            """);

        using var store = new FileDocumentStore(new FileDocumentStoreOptions { RootPath = _dir.Path });
        var service = new TimeEntryService(store);
        var old = Assert.Single(await service.GetRangeAsync(Day, Day));
        Assert.Equal(TimeSpan.FromHours(2.5), old.Duration);

        await service.SaveAsync(old);
        var json = await File.ReadAllTextAsync(Path.Combine(folder, "old.json"));
        Assert.DoesNotContain("\"start\"", json);
        Assert.Contains("\"duration\"", json);
    }

    [Fact]
    public async Task Delete_removes_the_entry()
    {
        var entry = Entry("ABC-1", 1);
        await _service.SaveAsync(entry);

        await _service.DeleteAsync(entry.Id);

        Assert.Empty(await _service.GetRangeAsync(Day, Day));
    }
}
