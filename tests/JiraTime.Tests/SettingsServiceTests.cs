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
    }
}
