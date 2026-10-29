using JiraTime.Domain;
using JiraTime.Storage;

namespace JiraTime.Tests;

public class FileDocumentStoreTests : IDisposable
{
    private readonly TempStore _files = new();

    public void Dispose() => _files.Dispose();

    private static TimeEntry Entry(string key, DateOnly date) =>
        new() { IssueKey = key, Date = date, Duration = TimeSpan.FromHours(1) };

    [Fact]
    public async Task Upsert_writes_one_json_file_per_document_in_the_collection_folder()
    {
        var entry = Entry("ABC-1", new(2026, 10, 19));

        await _files.Store.Collection<TimeEntry>().UpsertAsync(entry);

        Assert.True(File.Exists(Path.Combine(_files.Path, "time-entries", entry.Id + ".json")));
    }

    [Fact]
    public async Task Get_returns_the_stored_document_or_null()
    {
        var docs = _files.Store.Collection<TimeEntry>();
        var entry = Entry("ABC-1", new(2026, 10, 19));
        await docs.UpsertAsync(entry);

        var loaded = await docs.GetAsync(entry.Id);

        Assert.NotNull(loaded);
        Assert.Equal("ABC-1", loaded.IssueKey);
        Assert.Null(await docs.GetAsync("missing"));
    }

    [Fact]
    public async Task Documents_survive_a_new_store_instance()
    {
        var entry = Entry("ABC-2", new(2026, 10, 20));
        await _files.Store.Collection<TimeEntry>().UpsertAsync(entry);

        var reopened = _files.Reopen();
        var loaded = await reopened.Collection<TimeEntry>().GetAsync(entry.Id);

        Assert.NotNull(loaded);
        Assert.Equal(entry.Date, loaded.Date);
        Assert.Equal(entry.Duration, loaded.Duration);
    }

    [Fact]
    public async Task Query_filters_with_the_predicate()
    {
        var docs = _files.Store.Collection<TimeEntry>();
        await docs.UpsertAsync(Entry("A-1", new(2026, 10, 19)));
        await docs.UpsertAsync(Entry("A-2", new(2026, 10, 20)));
        await docs.UpsertAsync(Entry("A-3", new(2026, 10, 21)));

        var from = new DateOnly(2026, 10, 20);
        var result = await docs.QueryAsync(e => e.Date >= from);

        Assert.Equal(["A-2", "A-3"], result.Select(e => e.IssueKey).Order());
        Assert.Equal(3, (await docs.AllAsync()).Count);
    }

    [Fact]
    public async Task Delete_removes_the_document_and_reports_whether_it_existed()
    {
        var docs = _files.Store.Collection<TimeEntry>();
        var entry = Entry("A-1", new(2026, 10, 19));
        await docs.UpsertAsync(entry);

        Assert.True(await docs.DeleteAsync(entry.Id));
        Assert.False(await docs.DeleteAsync(entry.Id));
        Assert.Null(await docs.GetAsync(entry.Id));
        Assert.False(File.Exists(Path.Combine(_files.Path, "time-entries", entry.Id + ".json")));
    }

    [Fact]
    public async Task Writes_and_reads_at_the_same_time_while_files_change_on_disk_all_succeed()
    {
        var docs = _files.Store.Collection<TimeEntry>();
        var day = new DateOnly(2026, 10, 19);
        var edited = Entry("B-1", day);
        await docs.UpsertAsync(edited);
        var editedFile = Path.Combine(_files.Path, "time-entries", edited.Id + ".json");
        var editedJson = await File.ReadAllTextAsync(editedFile);

        // Each write and each hand edit makes the file watcher drop the cache on another thread.
        var writers = Enumerable.Range(0, 300).Select(i => Task.Run(() => docs.UpsertAsync(Entry($"A-{i}", day.AddDays(i % 10)))));
        var readers = Enumerable.Range(0, 300).Select(_ => Task.Run(() => docs.QueryAsync(e => e.Date >= day)));
        var handEdits = Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                File.WriteAllText(editedFile + ".edit", editedJson);
                File.Move(editedFile + ".edit", editedFile, overwrite: true);
            }
        });
        await Task.WhenAll(writers.Cast<Task>().Concat(readers).Append(handEdits));

        Assert.Equal(300, (await docs.QueryAsync(e => e.IssueKey.StartsWith("A-"))).Count);
    }

    [Fact]
    public async Task A_document_edited_by_hand_is_read_again()
    {
        var docs = _files.Store.Collection<TimeEntry>();
        var entry = Entry("A-1", new(2026, 10, 19));
        await docs.UpsertAsync(entry);
        var file = Path.Combine(_files.Path, "time-entries", entry.Id + ".json");

        var text = (await File.ReadAllTextAsync(file)).Replace("A-1", "A-2");
        await File.WriteAllTextAsync(file + ".edit", text);
        File.Move(file + ".edit", file, overwrite: true);

        // The file watcher reports the edit on its own thread, a moment later.
        for (var i = 0; i < 100 && (await docs.GetAsync(entry.Id))!.IssueKey != "A-2"; i++) await Task.Delay(20);
        Assert.Equal("A-2", (await docs.GetAsync(entry.Id))!.IssueKey);
    }

    [Fact]
    public void Collections_that_are_never_read_hold_no_file_watcher()
    {
        // Each file watcher takes an inotify instance on Linux, often limited to 128 per user.
        var stores = new List<TempStore>();
        try
        {
            for (var i = 0; i < 200; i++)
            {
                stores.Add(new TempStore());
                stores[^1].Store.Collection<TimeEntry>();
            }
        }
        finally
        {
            stores.ForEach(s => s.Dispose());
        }
    }
}
