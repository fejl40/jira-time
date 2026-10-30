using JiraTime.Components;
using JiraTime.Domain;

namespace JiraTime.Tests;

public class LinksTests
{
    private static readonly DateOnly Today = new(2026, 10, 28);

    [Fact]
    public void Links_read_back_to_what_they_were_built_from()
    {
        var day = new DateOnly(2026, 9, 3);

        Assert.Equal(new TimesheetLink(Period.For(day), day), Links.Read(Links.To(day), Today));
        Assert.Equal(new TimesheetLink(Period.For(day), null), Links.Read(Links.To(Period.For(day)), Today));
    }

    [Theory]
    [InlineData("", "2026-10-16", null)]
    [InlineData("?day=2026-09-03", "2026-09-01", "2026-09-03")]
    [InlineData("period/2026-09-16/", "2026-09-16", null)]
    [InlineData("period/nonsense?day=2026-09-03", "2026-09-01", "2026-09-03")]
    [InlineData("PERIOD/2026-09-16#top", "2026-09-16", null)]
    [InlineData("?day=soon", "2026-10-16", null)]
    public void Timesheet_links_name_a_period_and_maybe_a_day(string link, string periodStart, string? day)
    {
        var read = Links.Read(link, Today);

        Assert.Equal(DateOnly.Parse(periodStart), read?.Period.Start);
        Assert.Equal(day is null ? null : DateOnly.Parse(day), read?.Day);
    }

    [Fact]
    public void A_day_in_the_period_is_selected()
    {
        var day = new DateOnly(2026, 9, 3);
        Assert.Equal(day, new TimesheetLink(Period.For(day), day).Selected(Today));
    }

    [Fact]
    public void Without_a_day_in_the_period_today_is_selected_when_it_is_in_the_period()
    {
        var period = Period.For(Today);
        Assert.Equal(Today, new TimesheetLink(period, null).Selected(Today));
        Assert.Equal(Today, new TimesheetLink(period, new DateOnly(2026, 9, 3)).Selected(Today));
    }

    [Fact]
    public void Otherwise_the_first_day_of_the_period_is_selected()
    {
        var period = Period.For(new DateOnly(2026, 9, 20));
        Assert.Equal(period.Start, new TimesheetLink(period, null).Selected(Today));
        Assert.Equal(period.Start, new TimesheetLink(period, new DateOnly(2026, 9, 3)).Selected(Today));
    }

    [Theory]
    [InlineData("settings")]
    [InlineData("setup/connect")]
    [InlineData("periods")]
    public void Other_pages_are_not_timesheet_links(string link) => Assert.Null(Links.Read(link, Today));

    [Fact]
    public void Export_links_name_the_month() =>
        Assert.Equal("export?month=2026-02", ExportLinks.To(new DateOnly(2026, 2, 16)));

    [Theory]
    [InlineData("2026-02", "2026-02-01")]
    [InlineData(" 2026-12 ", "2026-12-01")]
    [InlineData("2026-13", null)]
    [InlineData("feb", null)]
    [InlineData(null, null)]
    public void Export_links_are_read_back(string? month, string? first) =>
        Assert.Equal(first is null ? null : DateOnly.Parse(first), ExportLinks.Read(month));

    [Fact]
    public void A_log_link_opens_the_requests_day_with_it_expanded()
    {
        var entry = new JiraLogEntry { Id = "abc123", Time = new DateTimeOffset(2026, 10, 30, 23, 30, 0, TimeSpan.FromHours(1)) };

        Assert.Equal("logs?day=2026-10-30&request=abc123", LogLinks.To(entry));
        Assert.Equal("logs?day=2026-10-30", LogLinks.To(new DateOnly(2026, 10, 30)));
    }
}
