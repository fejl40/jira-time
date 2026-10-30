using System.Globalization;
using JiraTime.Domain;

namespace JiraTime.Services.Jira;

/// <summary>Builds pieces of JQL so values are always quoted the same, safe way.</summary>
public static class Jql
{
    /// <summary>A JQL string literal: wrapped in double quotes, with backslash and double quote escaped.</summary>
    public static string Quote(string value) =>
        $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";

    /// <summary>A date literal Jira understands, e.g. "2026-10-19".</summary>
    public static string Date(DateOnly date) =>
        Quote(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

    /// <summary>The user as JQL names them: the username on Server/DC, the account id on Cloud.</summary>
    public static string User(JiraProfile profile) => Quote(profile.Username ?? profile.Id);

    /// <summary>A quoted, comma separated list in parentheses, e.g. ("ABC-1", "ABC-2").</summary>
    public static string List(IEnumerable<string> values) => $"({string.Join(", ", values.Select(Quote))})";
}
