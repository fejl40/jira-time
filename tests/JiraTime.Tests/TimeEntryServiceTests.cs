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

    private static TimeEntry Entry(string key, string start, string end, DateOnly? date = null) =>
        new() { IssueKey = key, Date = date ?? Day, Start = TimeOnly.Parse(start), End = TimeOnly.Parse(end) };

    [Fact]
    public async Task Saves_a_valid_entry()
    {
        Assert.Null(await _service.SaveAsync(Entry("ABC-1", "09:00", "10:30")));

        var entries = await _service.GetRangeAsync(Day, Day);
        Assert.Single(entries);
        Assert.Equal(TimeSpan.FromMinutes(90), entries[0].Duration);
    }

    [Fact]
    public async Task Rejects_a_missing_issue()
    {
        var error = await _service.SaveAsync(Entry(" ", "09:00", "10:00"));

        Assert.NotNull(error);
        Assert.Empty(await _service.GetRangeAsync(Day, Day));
    }

    [Theory]
    [InlineData("10:00", "10:00")]
    [InlineData("10:00", "09:00")]
    public async Task Rejects_end_not_after_start(string start, string end)
    {
        Assert.NotNull(await _service.SaveAsync(Entry("ABC-1", start, end)));
    }

    [Theory]
    [InlineData("09:30", "10:30")] // overlaps the end
    [InlineData("08:00", "09:01")] // overlaps the start
    [InlineData("09:15", "09:45")] // inside
    [InlineData("08:00", "11:00")] // around
    public async Task Rejects_overlap_on_the_same_day(string start, string end)
    {
        await _service.SaveAsync(Entry("ABC-1", "09:00", "10:00"));

        var error = await _service.SaveAsync(Entry("ABC-2", start, end));

        Assert.NotNull(error);
        Assert.Contains("ABC-1", error);
    }

    [Fact]
    public async Task Allows_back_to_back_entries_and_other_days()
    {
        await _service.SaveAsync(Entry("ABC-1", "09:00", "10:00"));

        Assert.Null(await _service.SaveAsync(Entry("ABC-2", "10:00", "11:00")));
        Assert.Null(await _service.SaveAsync(Entry("ABC-3", "08:00", "09:00")));
        Assert.Null(await _service.SaveAsync(Entry("ABC-4", "09:00", "10:00", Day.AddDays(1))));
    }

    [Fact]
    public async Task Editing_an_entry_does_not_clash_with_itself()
    {
        var entry = Entry("ABC-1", "09:00", "10:00");
        await _service.SaveAsync(entry);

        entry.End = new TimeOnly(10, 30);

        Assert.Null(await _service.SaveAsync(entry));
        Assert.Single(await _service.GetRangeAsync(Day, Day));
    }

    [Fact]
    public async Task GetRange_returns_entries_in_range_sorted_by_date_and_start()
    {
        await _service.SaveAsync(Entry("C", "13:00", "14:00"));
        await _service.SaveAsync(Entry("A", "08:00", "09:00"));
        await _service.SaveAsync(Entry("D", "08:00", "09:00", Day.AddDays(1)));
        await _service.SaveAsync(Entry("X", "08:00", "09:00", Day.AddDays(14)));

        var entries = await _service.GetRangeAsync(Day, Day.AddDays(13));

        Assert.Equal(["A", "C", "D"], entries.Select(e => e.IssueKey));
    }

    [Fact]
    public async Task Delete_removes_the_entry()
    {
        var entry = Entry("ABC-1", "09:00", "10:00");
        await _service.SaveAsync(entry);

        await _service.DeleteAsync(entry.Id);

        Assert.Empty(await _service.GetRangeAsync(Day, Day));
    }
}
