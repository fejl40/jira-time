using JiraTime.Domain;
using JiraTime.Storage;
using static JiraTime.Tests.Support.TestEntries;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Storage;

public class FileDocumentStoreTests : IDisposable
{
    private readonly TempStore _files = new();

    public void Dispose() => _files.Dispose();

    [Fact]
    public async Task Each_document_is_written_as_one_json_file_in_its_collection_folder()
    {
        var entry = Draft("ABC-1", 1, new(2026, 10, 19));

        await _files.Store.Collection<TimeEntry>().UpsertAsync(entry);

        Assert.True(File.Exists(Path.Combine(_files.Path, "time-entries", entry.Id + ".json")));
    }

    [Fact]
    public async Task A_document_is_read_back_by_id_and_a_missing_one_is_null()
    {
        var docs = _files.Store.Collection<TimeEntry>();
        var entry = Draft("ABC-1", 1, new(2026, 10, 19));
        await docs.UpsertAsync(entry);

        var loaded = await docs.GetAsync(entry.Id);

        Assert.NotNull(loaded);
        Assert.Equal("ABC-1", loaded.IssueKey);
        Assert.Null(await docs.GetAsync("missing"));
    }

    [Fact]
    public async Task Documents_survive_a_new_store_instance()
    {
        var entry = Draft("ABC-2", 1, new(2026, 10, 20));
        await _files.Store.Collection<TimeEntry>().UpsertAsync(entry);

        var reopened = _files.Reopen();
        var loaded = await reopened.Collection<TimeEntry>().GetAsync(entry.Id);

        Assert.NotNull(loaded);
        Assert.Equal(entry.Date, loaded.Date);
        Assert.Equal(entry.Duration, loaded.Duration);
    }

    [Fact]
    public async Task A_query_returns_only_the_documents_its_filter_matches()
    {
        var docs = _files.Store.Collection<TimeEntry>();
        await docs.UpsertAsync(Draft("A-1", 1, new(2026, 10, 19)));
        await docs.UpsertAsync(Draft("A-2", 1, new(2026, 10, 20)));
        await docs.UpsertAsync(Draft("A-3", 1, new(2026, 10, 21)));

        var from = new DateOnly(2026, 10, 20);
        var result = await docs.QueryAsync(e => e.Date >= from);

        Assert.Equal(["A-2", "A-3"], result.Select(e => e.IssueKey).Order());
        Assert.Equal(3, (await docs.QueryAsync(_ => true)).Count);
    }

    [Fact]
    public async Task Deleting_removes_the_document_and_says_whether_it_was_there()
    {
        var docs = _files.Store.Collection<TimeEntry>();
        var entry = Draft("A-1", 1, new(2026, 10, 19));
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
        var edited = Draft("B-1", 1, day);
        await docs.UpsertAsync(edited);
        var editedFile = Path.Combine(_files.Path, "time-entries", edited.Id + ".json");
        var editedJson = await File.ReadAllTextAsync(editedFile);

        // Each write and each hand edit makes the file watcher drop the cache on another thread.
        var writers = Enumerable.Range(0, 300).Select(i => Task.Run(() => docs.UpsertAsync(Draft($"A-{i}", 1, day.AddDays(i % 10)))));
        var readers = Enumerable.Range(0, 300).Select(_ => Task.Run(() => docs.QueryAsync(e => e.Date >= day)));
        // An editor saves the way the store does; on Windows it waits a moment while the file is in use.
        var handEdits = Task.Run(async () =>
        {
            for (var i = 0; i < 100; i++)
            {
                await File.WriteAllTextAsync(editedFile + ".edit", editedJson);
                await SharedFile.ReplaceAsync(editedFile + ".edit", editedFile);
            }
        });
        await Task.WhenAll(writers.Cast<Task>().Concat(readers).Append(handEdits));

        Assert.Equal(300, (await docs.QueryAsync(e => e.IssueKey.StartsWith("A-"))).Count);
    }

    [Fact]
    public async Task A_document_edited_by_hand_is_read_again()
    {
        var docs = _files.Store.Collection<TimeEntry>();
        var entry = Draft("A-1", 1, new(2026, 10, 19));
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
