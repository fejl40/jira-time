using JiraTime.Components;

namespace JiraTime.Tests;

public class FormatTests
{
    [Theory]
    [InlineData("1.5", 90)]
    [InlineData("1,5", 90)]
    [InlineData(" 2 ", 120)]
    [InlineData("0.25", 15)]
    [InlineData("1:30", 90)]
    [InlineData("0:05", 5)]
    [InlineData("7.5h", 450)]
    [InlineData("1.333", 80)]
    public void Reads_hours(string text, int minutes) =>
        Assert.Equal(TimeSpan.FromMinutes(minutes), Format.ParseHours(text));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1:75")]
    [InlineData("1:2:3")]
    public void Rejects_unreadable_or_non_positive_hours(string? text) =>
        Assert.Null(Format.ParseHours(text));
}
