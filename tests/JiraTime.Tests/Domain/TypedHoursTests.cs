using JiraTime.Domain;

namespace JiraTime.Tests;

/// <summary>Reading hours as the user types them.</summary>
public class TypedHoursTests
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
    public void Typed_hours_are_read_with_a_point_a_comma_or_a_colon(string text, int minutes) =>
        Assert.Equal(TimeSpan.FromMinutes(minutes), TypedHours.Parse(text));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1:75")]
    [InlineData("1:2:3")]
    public void Rejects_unreadable_or_non_positive_hours(string? text) =>
        Assert.Null(TypedHours.Parse(text));
}
