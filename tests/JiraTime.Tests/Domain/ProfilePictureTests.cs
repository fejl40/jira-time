using JiraTime.Domain;

namespace JiraTime.Tests.Domain;

public class ProfilePictureTests
{
    [Fact]
    public void A_picture_the_user_uploaded_to_Jira_Server_is_read()
    {
        const string uploaded = "https://jira.example.com/secure/useravatar?ownerId=carl&avatarId=10500";

        Assert.Equal(uploaded, ProfilePicture.SourceFor(uploaded));
    }

    [Theory]
    [InlineData("https://jira.example.com/secure/useravatar?avatarId=10122")]
    [InlineData("https://jira.example.com/images/icons/avatars/default.png")]
    [InlineData("https://avatar-management--avatars.us-west-2.prod.public.atl-paas.net/initials/CL-3.png")]
    [InlineData("https://avatar-management--avatars.us-west-2.prod.public.atl-paas.net/default-avatar.png")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("javascript:alert(1)")]
    public void Jiras_default_avatar_or_none_keeps_the_initials(string? avatarUrl) =>
        Assert.Null(ProfilePicture.SourceFor(avatarUrl));

    [Fact]
    public void A_picture_on_Cloud_is_read()
    {
        const string uploaded = "https://avatar-management--avatars.us-west-2.prod.public.atl-paas.net/557058:abc/1d2e3f/48";

        Assert.Equal(uploaded, ProfilePicture.SourceFor(uploaded));
    }

    [Fact]
    public void Gravatar_is_asked_for_a_404_instead_of_its_stand_in_picture()
    {
        var source = ProfilePicture.SourceFor("https://secure.gravatar.com/avatar/abc?d=https%3A%2F%2Favatar.example%2Finitials%2FCL-3.png&s=48");

        Assert.Equal("https://secure.gravatar.com/avatar/abc?s=48&d=404", source);
    }

    [Theory]
    [InlineData("image/png", 2000, true)]
    [InlineData("image/JPEG", 2000, true)]
    [InlineData("image/svg+xml", 2000, false)]
    [InlineData("text/html", 2000, false)]
    [InlineData(null, 2000, false)]
    [InlineData("image/png", 0, false)]
    [InlineData("image/png", ProfilePicture.MaxBytes + 1, false)]
    public void Only_plain_images_of_a_sensible_size_are_shown(string? contentType, int length, bool shown) =>
        Assert.Equal(shown, ProfilePicture.CanShow(contentType, length));

    [Fact]
    public void A_picture_is_fresh_for_two_days_from_the_same_address()
    {
        var read = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);
        var picture = new ProfilePicture { SourceUrl = "https://a/1", ReadUtc = read };

        Assert.True(picture.IsFresh("https://a/1", read.AddDays(2).AddMinutes(-1)));
        Assert.False(picture.IsFresh("https://a/1", read.AddDays(2)));
        Assert.False(picture.IsFresh("https://a/2", read));
    }

    [Fact]
    public void Without_a_picture_there_is_no_app_URL()
    {
        Assert.Null(new ProfilePicture { SourceUrl = "https://a/1" }.Path);
        Assert.Equal("profile-picture/k", new ProfilePicture { Key = "k", Bytes = [1], ContentType = "image/png" }.Path);
    }
}
