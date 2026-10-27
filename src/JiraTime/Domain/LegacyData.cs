using JiraTime.Storage;

namespace JiraTime.Domain;

/// <summary>
/// The single settings document from before multi-user support. Only read to find out whose the old
/// data is; the profile now comes from Jira on each visit.
/// </summary>
[DocumentCollection("settings")]
public class AppSettings : IDocument
{
    public const string SingletonId = "app";
    public string Id { get; set; } = SingletonId;

    public JiraProfile? Profile { get; set; }
}

/// <summary>Copy of the data from before multi-user support, stored before it was given to its owner.</summary>
[DocumentCollection("backups")]
public class LegacyBackup : IDocument
{
    public string Id { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public UserKey? ClaimedBy { get; set; }
    public List<TimeEntry> Entries { get; set; } = [];
    public List<DayOff> DaysOff { get; set; } = [];
    public AppSettings? Settings { get; set; }
}
