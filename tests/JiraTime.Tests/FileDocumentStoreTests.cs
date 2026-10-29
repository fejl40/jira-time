using JiraTime.Domain;
using JiraTime.Storage;

namespace JiraTime.Tests;

public class FileDocumentStoreTests : IDisposable
{
    private readonly TempDir _dir = new();

    public void Dispose() => _dir.Dispose();

    private FileDocumentStore NewStore() => new(_dir.Path);

    private static TimeEntry Entry(string key, DateOnly date) =>
        new() { IssueKey = key, Date = date, Duration = TimeSpan.FromHours(1) };

    [Fact]
    public async Task Upsert_writes_one_json_file_per_document_in_the_collection_folder()
    {
        using var store = NewStore();
        var entry = Entry("ABC-1", new(2026, 10, 19));

        await store.Collection<TimeEntry>().UpsertAsync(entry);

        Assert.True(File.Exists(Path.Combine(_dir.Path, "time-entries", entry.Id + ".json")));
    }

    [Fact]
    public async Task Get_returns_the_stored_document_or_null()
    {
        using var store = NewStore();
        var docs = store.Collection<TimeEntry>();
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
        using (var store = NewStore())
            await store.Collection<TimeEntry>().UpsertAsync(entry);

        using var reopened = NewStore();
        var loaded = await reopened.Collection<TimeEntry>().GetAsync(entry.Id);

        Assert.NotNull(loaded);
        Assert.Equal(entry.Date, loaded.Date);
        Assert.Equal(entry.Duration, loaded.Duration);
    }

    [Fact]
    public async Task Query_filters_with_the_predicate()
    {
        using var store = NewStore();
        var docs = store.Collection<TimeEntry>();
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
        using var store = NewStore();
        var docs = store.Collection<TimeEntry>();
        var entry = Entry("A-1", new(2026, 10, 19));
        await docs.UpsertAsync(entry);

        Assert.True(await docs.DeleteAsync(entry.Id));
        Assert.False(await docs.DeleteAsync(entry.Id));
        Assert.Null(await docs.GetAsync(entry.Id));
        Assert.False(File.Exists(Path.Combine(_dir.Path, "time-entries", entry.Id + ".json")));
    }

    [Fact]
    public async Task Writes_and_reads_at_the_same_time_while_files_change_on_disk_all_succeed()
    {
        using var store = NewStore();
        var docs = store.Collection<TimeEntry>();
        var day = new DateOnly(2026, 10, 19);
        var edited = Entry("B-1", day);
        await docs.UpsertAsync(edited);
        var editedFile = Path.Combine(_dir.Path, "time-entries", edited.Id + ".json");
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
        using var store = NewStore();
        var docs = store.Collection<TimeEntry>();
        var entry = Entry("A-1", new(2026, 10, 19));
        await docs.UpsertAsync(entry);
        var file = Path.Combine(_dir.Path, "time-entries", entry.Id + ".json");

        var text = (await File.ReadAllTextAsync(file)).Replace("A-1", "A-2");
        await File.WriteAllTextAsync(file + ".edit", text);
        File.Move(file + ".edit", file, overwrite: true);

        // The file watcher reports the edit on its own thread, a moment later.
        for (var i = 0; i < 100 && (await docs.GetAsync(entry.Id))!.IssueKey != "A-2"; i++) await Task.Delay(20);
        Assert.Equal("A-2", (await docs.GetAsync(entry.Id))!.IssueKey);
    }
}
