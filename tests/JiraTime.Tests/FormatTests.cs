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

    [Theory]
    [InlineData(0, "0")]
    [InlineData(90, "1.5")]
    [InlineData(20, "0.3333")]
    [InlineData(480, "8")]
    public void Hours_for_scripts_are_never_empty_and_use_a_dot(int minutes, string expected) =>
        Assert.Equal(expected, Format.HoursValue(TimeSpan.FromMinutes(minutes)));

    [Fact]
    public void Hours_text_and_days_read_well()
    {
        Assert.Equal("0 h", Format.HoursText(TimeSpan.Zero));
        Assert.Equal("7.5 h", Format.HoursText(TimeSpan.FromHours(7.5)));
        Assert.Equal("1 day", Format.Days(1));
        Assert.Equal("3 days", Format.Days(3));
    }
}
