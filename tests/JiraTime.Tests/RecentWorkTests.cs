using JiraTime.Domain;

namespace JiraTime.Tests;

public class RecentWorkTests
{
    private static readonly DateOnly Today = new(2026, 10, 27);

    private static TimeEntry Logged(string key, int daysAgo, string? comment = null) =>
        new() { IssueKey = key, IssueSummary = key + " summary", Date = Today.AddDays(-daysAgo), Duration = TimeSpan.FromHours(1), Comment = comment };

    [Fact]
    public void Recent_issues_are_newest_first_and_each_once()
    {
        var issues = RecentWork.Issues([Logged("ABC-1", 5), Logged("ABC-2", 1), Logged("abc-1", 2), Logged("", 0)]);

        Assert.Equal(["ABC-2", "abc-1"], issues.Select(i => i.Key));
    }

    [Fact]
    public void Recent_issues_are_capped() =>
        Assert.Equal(3, RecentWork.Issues(Enumerable.Range(1, 10).Select(i => Logged($"ABC-{i}", i)), count: 3).Count);

    [Fact]
    public void Recent_comments_are_per_issue_newest_first_and_distinct()
    {
        var comments = RecentWork.Comments([
            Logged("ABC-1", 3, "Standup"), Logged("ABC-1", 2, "Code review"), Logged("ABC-1", 1, "standup"),
            Logged("ABC-1", 1), Logged("ABC-2", 1, "Planning"),
        ]);

        Assert.Equal(["standup", "Code review"], comments["ABC-1"]);
        Assert.Equal(["Planning"], comments["abc-2"]);
    }

    [Fact]
    public void Recent_comments_are_capped_per_issue() =>
        Assert.Equal(["Comment 1", "Comment 2", "Comment 3"],
            RecentWork.Comments(Enumerable.Range(1, 7).Select(i => Logged("ABC-1", i, $"Comment {i}")), perIssue: 3)["ABC-1"]);
}
