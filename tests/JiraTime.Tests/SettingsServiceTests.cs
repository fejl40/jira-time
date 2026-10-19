using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Storage.FileSystem;

namespace JiraTime.Tests;

public class SettingsServiceTests
{
    [Fact]
    public async Task Loads_a_settings_file_written_before_period_anchors_were_removed()
    {
        using var dir = new TempDir();
        var folder = Path.Combine(dir.Path, "settings");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "app.json"), """
            {
              "id": "app",
              "profile": { "id": "JIRAUSER1", "username": "carl", "displayName": "Carl Lilholm", "email": null },
              "periodAnchor": "2026-01-05"
            }
            """);

        using var store = new FileDocumentStore(new FileDocumentStoreOptions { RootPath = dir.Path });
        var settings = await new SettingsService(store).GetAsync();

        Assert.Equal("Carl Lilholm", settings.Profile?.DisplayName);
        Assert.Equal(Workday.Default, settings.Workday);
    }

    [Fact]
    public async Task Workday_is_saved_and_read_back()
    {
        using var dir = new TempDir();
        using var store = new FileDocumentStore(new FileDocumentStoreOptions { RootPath = dir.Path });
        var w = new Workday(new(7, 30), new(16, 0), new(11, 30), new(12, 0));

        await new SettingsService(store).SaveWorkdayAsync(w);

        using var reopened = new FileDocumentStore(new FileDocumentStoreOptions { RootPath = dir.Path });
        Assert.Equal(w, (await new SettingsService(reopened).GetAsync()).Workday);
    }
}
