using JiraTime.Domain;
using JiraTime.Services;

namespace JiraTime.Tests;

public static class TestUsers
{
    public static readonly UserKey Carl = UserKey.For("https://jira.example.com", "JIRAUSER1");
    public static readonly UserKey Anna = UserKey.For("https://jira.example.com", "JIRAUSER2");

    public static CurrentUser SignedIn(UserKey? key = null) => CurrentUser.SignedIn(key ?? Carl);
}
