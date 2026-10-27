using JiraTime.Domain;

namespace JiraTime.Tests;

public class IssueChoicesTests
{
    private static IssueWithStatus S(string key, string summary, string category = "new") => new(key, summary, "Task", category == "done" ? "Done" : "To Do", category);

    private static readonly IssueChoices Choices = new()
    {
        Sprint = [S("ABC-1", "Login page"), S("ABC-2", "Build")],
        Earlier = [S("ABC-2", "Build"), S("ABC-4", "Old report", "done")],
        Recent = [new("ABC-1", "Login page"), new("XYZ-9", "Support"), new("", "")],
        Statuses = new Dictionary<string, IssueWithStatus> { ["XYZ-9"] = S("XYZ-9", "Support", "done") },
    };

    private static string Show(IReadOnlyList<IssueGroup> groups) =>
        string.Join(" | ", groups.Select(g => $"{g.Title}: {string.Join(", ", g.Items.Select(i => i.Key))}"));

    [Fact]
    public void Each_issue_is_listed_once_in_the_first_group_it_belongs_to()
    {
        Assert.Equal("My sprint: ABC-1, ABC-2 | Earlier this period: ABC-4 | Recent: XYZ-9", Show(Choices.For(null, [])));
    }

    [Fact]
    public void Typing_filters_every_group_and_adds_jira_results_not_shown_yet()
    {
        var jira = new JiraIssue[] { new("ABC-4", "Old report"), new("ABC-7", "Report export") };

        Assert.Equal("Earlier this period: ABC-4 | Jira search: ABC-7", Show(Choices.For("report", jira)));
    }

    [Fact]
    public void Jira_results_wait_for_two_characters()
    {
        Assert.DoesNotContain("Jira search", Show(Choices.For("A", [new("DEF-1", "Anything")])));
    }

    [Fact]
    public void A_jira_result_hidden_by_the_filter_in_its_own_group_still_shows()
    {
        // Jira matched on something other than key or summary.
        Assert.Equal("Jira search: ABC-1", Show(Choices.For("frontend", [new("ABC-1", "Login page")])));
    }

    [Fact]
    public void Status_comes_from_the_lists_then_the_lookups_ignoring_case()
    {
        Assert.Equal("To Do", Choices.StatusOf("abc-1")?.Status);
        Assert.True(Choices.StatusOf("ABC-4")?.IsDone);
        Assert.True(Choices.StatusOf("XYZ-9")?.IsDone);
        Assert.Null(Choices.StatusOf("NOPE-1"));
    }
}
