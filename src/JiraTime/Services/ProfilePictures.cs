using System.Net;
using System.Security.Cryptography;
using JiraTime.Domain;
using JiraTime.Services.Jira;
using JiraTime.Services.JiraLog;
using JiraTime.Services.SignIn;
using JiraTime.Storage;

namespace JiraTime.Services;

/// <summary>
/// The signed-in user's Jira profile picture for the header: read from Jira through <see cref="JiraHttp"/> at most
/// once per <see cref="ProfilePicture.KeepFor"/> and kept with the user's documents, so the browser can load it
/// from the app without the token.
/// </summary>
public class ProfilePictures(IDocumentStore store, CurrentUser user, JiraHttp jira, TimeProvider clock)
{
    private readonly OwnedCollection<ProfilePicture> _pictures = new(store, user);

    /// <summary>The picture's app URL; null when the initials should show (no picture, Jira's default, or it can't be read).</summary>
    public async Task<string?> GetUrlAsync(CancellationToken ct = default)
    {
        if (user.Jira is not { } signedIn || ProfilePicture.SourceFor(signedIn.Profile.AvatarUrl) is not { } source) return null;

        var now = clock.GetUtcNow().UtcDateTime;
        var kept = await _pictures.GetAsync(_pictures.Owner);
        var keptPath = kept?.SourceUrl == source ? kept.Path : null;
        if (user.VerifiedOffline || kept?.IsFresh(source, now) == true) return keptPath;

        using var action = JiraAction.Start("Profile picture");
        var read = await ReadAsync(signedIn.Connection, source, now, ct);
        if (read is null) return keptPath; // Jira couldn't be reached: the picture read before still shows
        await _pictures.UpsertAsync(read);
        return read.Path;
    }

    /// <summary>The picture as Jira answered (without one when it answered with none); null when Jira couldn't be asked.</summary>
    private async Task<ProfilePicture?> ReadAsync(JiraConnection c, string source, DateTime now, CancellationToken ct)
    {
        var picture = new ProfilePicture { Id = _pictures.Owner, SourceUrl = source, ReadUtc = now };
        try
        {
            var answer = await jira.GetPictureAsync(c, new Uri(source), ct);
            if (!ProfilePicture.CanShow(answer.ContentType, answer.Bytes.Length)) return picture;
            picture.Bytes = answer.Bytes;
            picture.ContentType = answer.ContentType!.ToLowerInvariant();
            picture.Key = RandomNumberGenerator.GetHexString(32, lowercase: true);
            return picture;
        }
        catch (JiraException ex) when (ex.Status is { } status && status != HttpStatusCode.TooManyRequests)
        {
            return picture; // Jira answered without a picture: not asked again until the picture is due
        }
        catch (JiraException)
        {
            return null;
        }
    }

    /// <summary>
    /// Serves a picture by its key. The request has no signed-in user (it isn't part of a tab's circuit), so the key is
    /// the permission: 128 random bits, new on every read, written only into its owner's own pages.
    /// </summary>
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("profile-picture/{key}", async (string key, IDocumentStore documents, HttpContext http) =>
        {
            if (await FindAsync(documents, key) is not { } picture)
            {
                http.Response.Headers.CacheControl = "no-store";
                return Results.NotFound();
            }
            http.Response.Headers.CacheControl = $"private, max-age={(int)ProfilePicture.KeepFor.TotalSeconds}, immutable";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            http.Response.Headers.ContentSecurityPolicy = "default-src 'none'";
            return Results.File(picture.Bytes!, picture.ContentType);
        });

    /// <summary>The picture whose app URL has this key; null for any other key.</summary>
    public static async Task<ProfilePicture?> FindAsync(IDocumentStore documents, string key)
    {
        if (key.Length != 32) return null;
        var found = await documents.Collection<ProfilePicture>().QueryAsync(p => p.Key == key);
        return found is [{ HasPicture: true } picture] ? picture : null;
    }
}
