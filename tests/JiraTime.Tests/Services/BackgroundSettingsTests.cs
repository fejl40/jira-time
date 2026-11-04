using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Services;

/// <summary>The background picked on Settings, kept in the user's settings document.</summary>
public sealed class BackgroundSettingsTests : IDisposable
{
    private readonly TimeHarness _harness = new();

    private BackgroundSettings Background => _harness.Get<BackgroundSettings>();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public async Task Aurora_is_the_default() => Assert.Equal(BackgroundChoice.Aurora, await Background.GetAsync());

    [Fact]
    public async Task Nobody_signed_in_sees_the_aurora()
    {
        var signedOut = new BackgroundSettings(_harness.Store, new JiraTime.Services.SignIn.CurrentUser());

        Assert.Equal(BackgroundChoice.Aurora, await signedOut.GetAsync());
    }

    [Fact]
    public async Task The_choice_is_saved_per_user_and_read_back_later()
    {
        var changes = 0;
        Background.Changed += () => changes++;

        await Background.SaveAsync(BackgroundChoice.Sky);

        var laterTab = new BackgroundSettings(_harness.Store, TestUsers.SignedIn(TestUsers.Carl));
        var anna = new BackgroundSettings(_harness.Store, TestUsers.SignedIn(TestUsers.Anna));
        Assert.Equal(BackgroundChoice.Sky, await laterTab.GetAsync());
        Assert.Equal(BackgroundChoice.Aurora, await anna.GetAsync());
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task Work_hours_and_background_keep_each_other()
    {
        var fourDays = UserSettings.StandardWeek();
        fourDays[DayOfWeek.Friday] = TimeSpan.Zero;

        await Background.SaveAsync(BackgroundChoice.Plain);
        await _harness.Calendar.SaveWorkHoursAsync(fourDays);

        Assert.Equal(BackgroundChoice.Plain, await Background.GetAsync());
        Assert.Equal(TimeSpan.Zero, (await _harness.Calendar.GetSettingsAsync()).HoursOn(DayOfWeek.Friday));
    }
}
