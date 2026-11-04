using JiraTime.Domain;
using JiraTime.Services.SignIn;
using JiraTime.Storage;

namespace JiraTime.Services;

/// <summary>
/// The background the signed-in user picked on Settings, kept in their settings document. Tells the tab's background
/// layer when it changes, so it switches without a reload.
/// </summary>
public sealed class BackgroundSettings(IDocumentStore store, CurrentUser user)
{
    private readonly OwnedCollection<UserSettings> _settings = new(store, user);

    public event Action? Changed;

    /// <summary>The user's background; Aurora when they never picked one or nobody is signed in.</summary>
    public async Task<BackgroundChoice> GetAsync()
    {
        if (!user.IsSignedIn) return BackgroundChoice.Aurora;
        return (await _settings.GetAsync(user.Owner))?.Background ?? BackgroundChoice.Aurora;
    }

    public async Task SaveAsync(BackgroundChoice choice)
    {
        var settings = await _settings.GetAsync(user.Owner) ?? new UserSettings { Id = user.Owner, Owner = user.Owner };
        settings.Background = choice;
        await _settings.UpsertAsync(settings);
        Changed?.Invoke();
    }
}
