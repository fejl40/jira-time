using JiraTime.Domain;
using static JiraTime.Tests.TestEntries;

namespace JiraTime.Tests;

public class RecentWorkTests
{
    private static readonly DateOnly Today = new(2026, 10, 27);

    private static DateOnly DaysAgo(int days) => Today.AddDays(-days);

    [Fact]
    public void Recent_issues_are_newest_first_and_each_once()
    {
        var issues = RecentWork.Issues([Saved("ABC-1", 1, DaysAgo(5)), Saved("ABC-2", 1, DaysAgo(1)), Saved("abc-1", 1, DaysAgo(2)), Saved("", 1, DaysAgo(0))]);

        Assert.Equal(["ABC-2", "abc-1"], issues.Select(i => i.Key));
    }

    [Fact]
    public void Recent_issues_are_capped() =>
        Assert.Equal(3, RecentWork.Issues(Enumerable.Range(1, 10).Select(i => Saved($"ABC-{i}", 1, DaysAgo(i))), count: 3).Count);

    [Fact]
    public void Recent_comments_are_per_issue_newest_first_and_distinct()
    {
        var comments = RecentWork.Comments([
            Saved("ABC-1", 1, DaysAgo(3), "Standup"), Saved("ABC-1", 1, DaysAgo(2), "Code review"), Saved("ABC-1", 1, DaysAgo(1), "standup"),
            Saved("ABC-1", 1, DaysAgo(1)), Saved("ABC-2", 1, DaysAgo(1), "Planning"),
        ]);

        Assert.Equal(["standup", "Code review"], comments["ABC-1"]);
        Assert.Equal(["Planning"], comments["abc-2"]);
    }

    [Fact]
    public void Recent_comments_are_capped_per_issue() =>
        Assert.Equal(["Comment 1", "Comment 2", "Comment 3"],
            RecentWork.Comments(Enumerable.Range(1, 7).Select(i => Saved("ABC-1", 1, DaysAgo(i), $"Comment {i}")), perIssue: 3)["ABC-1"]);
}
