using System.Net;
using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Services.SignIn;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Services;

/// <summary>The header's profile picture: read from Jira at most once per two days, logged, and only ever the user's own.</summary>
public class ProfilePicturesTests : IDisposable
{
    private const string AvatarPath = "/secure/useravatar";
    private const string CarlsAvatar = "https://jira.example.com/secure/useravatar?ownerId=carl&avatarId=10500";
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 1, 2, 3];

    private readonly TimeHarness _h = new();

    public ProfilePicturesTests() => _h.Jira.Pictures[AvatarPath] = (Png, "image/png");

    public void Dispose() => _h.Dispose();

    private int PictureReads => _h.Jira.Requests.Count(r => r.Path.StartsWith(AvatarPath));

    /// <summary>Signs Carl's tab in through the fake Jira, whose /myself names <paramref name="avatar"/>.</summary>
    private async Task<ProfilePictures> SignInWithAvatarAsync(string? avatar = CarlsAvatar)
    {
        _h.Jira.Myself = new { key = "JIRAUSER1", name = "carl", displayName = "Carl Lilholm", avatarUrls = new Dictionary<string, string?> { ["48x48"] = avatar } };
        await _h.Identity(_h.User).SignInAsync(_h.Connection);
        return _h.Get<ProfilePictures>();
    }

    [Fact]
    public async Task The_picture_is_read_once_and_reused_within_two_days()
    {
        var pictures = await SignInWithAvatarAsync();

        var first = await pictures.GetUrlAsync();
        _h.Clock.Advance(TimeSpan.FromDays(1));
        var again = await pictures.GetUrlAsync();

        Assert.NotNull(first);
        Assert.Equal(first, again);
        Assert.Equal(1, PictureReads);
        var served = await ProfilePictures.FindAsync(_h.Store, first["profile-picture/".Length..]);
        Assert.Equal(Png, served!.Bytes);
        Assert.Equal("image/png", served.ContentType);
    }

    [Fact]
    public async Task After_two_days_the_picture_is_read_again_at_a_new_URL()
    {
        var pictures = await SignInWithAvatarAsync();
        var first = await pictures.GetUrlAsync();

        _h.Clock.Advance(ProfilePicture.KeepFor);
        var later = await pictures.GetUrlAsync();

        Assert.Equal(2, PictureReads);
        Assert.NotEqual(first, later);
        Assert.Null(await ProfilePictures.FindAsync(_h.Store, first!["profile-picture/".Length..]));
    }

    [Fact]
    public async Task A_changed_avatar_in_Jira_is_read_at_once()
    {
        var first = await (await SignInWithAvatarAsync()).GetUrlAsync();

        var changed = await (await SignInWithAvatarAsync(CarlsAvatar + "&v=2")).GetUrlAsync();

        Assert.Equal(2, PictureReads);
        Assert.NotEqual(first, changed);
    }

    [Fact]
    public async Task The_picture_request_carries_the_token_and_is_in_the_request_log()
    {
        var pictures = await SignInWithAvatarAsync();

        await pictures.GetUrlAsync();

        Assert.True(_h.Jira.Requests.Single(r => r.Path.StartsWith(AvatarPath)).Authorized);
        var log = await _h.RequestLog.ReadDayAsync(TestUsers.Carl.Id, _h.Today);
        var entry = Assert.Single(log, e => e.Url.Contains(AvatarPath));
        Assert.Equal("Profile picture", entry.Action);
        Assert.Equal(200, entry.Status);
        Assert.Equal("image/png", entry.ContentType);
        Assert.Equal(Png.Length, entry.ResponseSize);
        Assert.Null(entry.ResponseBody);
    }

    [Fact]
    public async Task The_token_is_never_sent_to_another_host()
    {
        _h.Jira.Pictures["/avatar/abc"] = (Png, "image/png");
        var pictures = await SignInWithAvatarAsync("https://secure.gravatar.com/avatar/abc?d=mm");

        Assert.NotNull(await pictures.GetUrlAsync());

        var read = _h.Jira.Requests.Single(r => r.Path.StartsWith("/avatar/abc"));
        Assert.Equal("secure.gravatar.com", read.Host);
        Assert.False(read.Authorized);
    }

    [Fact]
    public async Task Jiras_default_avatar_keeps_the_initials_without_asking_Jira()
    {
        var pictures = await SignInWithAvatarAsync("https://jira.example.com/secure/useravatar?avatarId=10122");

        Assert.Null(await pictures.GetUrlAsync());
        Assert.Equal(0, PictureReads);
    }

    [Fact]
    public async Task A_failed_read_keeps_the_initials_and_is_not_retried_within_two_days()
    {
        var pictures = await SignInWithAvatarAsync();
        _h.Jira.FailWhen = uri => uri.AbsolutePath == AvatarPath ? HttpStatusCode.NotFound : null;

        Assert.Null(await pictures.GetUrlAsync());
        Assert.Null(await pictures.GetUrlAsync());
        Assert.Equal(1, PictureReads);
    }

    [Fact]
    public async Task A_picture_that_is_not_a_plain_image_keeps_the_initials()
    {
        _h.Jira.Pictures[AvatarPath] = ("<svg onload='alert(1)'/>"u8.ToArray(), "image/svg+xml");
        var pictures = await SignInWithAvatarAsync();

        Assert.Null(await pictures.GetUrlAsync());
    }

    [Fact]
    public async Task While_Jira_cant_be_reached_the_picture_read_before_still_shows_and_is_read_again_later()
    {
        var pictures = await SignInWithAvatarAsync();
        var first = await pictures.GetUrlAsync();
        _h.Clock.Advance(ProfilePicture.KeepFor);

        _h.Jira.Offline = true;
        var offline = await pictures.GetUrlAsync();
        _h.Jira.Offline = false;
        var back = await pictures.GetUrlAsync();

        Assert.Equal(first, offline);
        Assert.NotEqual(first, back);
        Assert.Equal(2, PictureReads);
    }

    [Fact]
    public async Task Each_user_only_gets_their_own_picture()
    {
        var carls = await (await SignInWithAvatarAsync()).GetUrlAsync();
        var anna = new CurrentUser();
        _h.Jira.Pictures["/secure/anna"] = ([9, 9, 9], "image/png");
        _h.Jira.Myself = new { key = "JIRAUSER2", name = "anna", displayName = "Anna Berg", avatarUrls = new Dictionary<string, string> { ["48x48"] = "https://jira.example.com/secure/anna" } };
        await _h.Identity(anna).SignInAsync(_h.Connection with { Token = "token-2" });

        var annas = await new ProfilePictures(_h.Store, anna, _h.Http, _h.Clock).GetUrlAsync();

        Assert.NotEqual(carls, annas);
        Assert.Equal([9, 9, 9], (await ProfilePictures.FindAsync(_h.Store, annas!["profile-picture/".Length..]))!.Bytes);
        Assert.Equal(Png, (await ProfilePictures.FindAsync(_h.Store, carls!["profile-picture/".Length..]))!.Bytes);
        Assert.Null(await ProfilePictures.FindAsync(_h.Store, TestUsers.Carl.Id));
        Assert.Null(await ProfilePictures.FindAsync(_h.Store, new string('0', 32)));
    }
}
