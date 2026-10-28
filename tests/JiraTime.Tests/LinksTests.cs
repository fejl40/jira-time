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

    [Theory]
    [InlineData("settings")]
    [InlineData("setup/connect")]
    [InlineData("periods")]
    public void Other_pages_are_not_timesheet_links(string link) => Assert.Null(Links.Read(link, Today));
}
