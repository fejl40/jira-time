using JiraTime.Components.Layout;
using JiraTime.Domain;
using JiraTime.Tests.Support;
using static JiraTime.Tests.Support.TestEntries;

namespace JiraTime.Tests.Components;

public class BackdropStateTests
{
    private static readonly Period Second = Period.For(new DateOnly(2026, 10, 16));

    private static PeriodSummary Complete() =>
        PeriodSummary.For(Second, TestSheets.Days(Second), [Saved("ABC-1", 8, new DateOnly(2026, 10, 16))], new DateOnly(2026, 10, 16));

    [Fact]
    public void A_tab_starts_neutral() => Assert.Equal(AuroraPalette.Neutral, new BackdropState().Palette);

    [Fact]
    public void A_period_shows_its_progress_and_leaving_it_goes_back_to_neutral()
    {
        var state = new BackdropState();
        var changes = 0;
        state.Changed += () => changes++;

        state.Show(Complete());
        state.Show(Complete());
        Assert.Equal((AuroraPalette.Gold, 1), (state.Palette, changes)); // the same palette again redraws nothing

        state.Show(null);
        Assert.Equal((AuroraPalette.Neutral, 2), (state.Palette, changes));
    }
}
