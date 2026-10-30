using System.Globalization;
using JiraTime.Services.Jira;

namespace JiraTime.Tests.Services.Jira;

/// <summary>Jira's worklog formats don't depend on the machine's language settings.</summary>
public class JiraWorklogJsonTests
{
    [Theory]
    [InlineData("da-DK")]
    [InlineData("fi-FI")]
    [InlineData("en-US")]
    public void Started_is_written_and_read_the_same_in_any_culture(string culture)
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(2), "Test", "Test");
        var before = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        try
        {
            var started = JiraWorklogJson.FormatStarted(new DateOnly(2026, 10, 19), new TimeOnly(9, 0), zone);

            Assert.Equal("2026-10-19T09:00:00.000+0200", started);
            Assert.Equal(new DateOnly(2026, 10, 19), JiraWorklogJson.ParseStarted(started));
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }
}
