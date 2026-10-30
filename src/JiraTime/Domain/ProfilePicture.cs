using System.Text.Json.Serialization;
using JiraTime.Storage;

namespace JiraTime.Domain;

/// <summary>
/// The user's Jira profile picture as the app keeps it (id = owner). It is read from Jira at most once per
/// <see cref="KeepFor"/> and served to the browser at <see cref="Path"/>: an unguessable key, new on every read,
/// that only this user's own pages show, so the key is both the permission and the version.
/// </summary>
[DocumentCollection("profile-pictures")]
public class ProfilePicture : IOwnedDocument
{
    /// <summary>How long a picture read from Jira is used before Jira is asked again, and how long the browser keeps it.</summary>
    public static readonly TimeSpan KeepFor = TimeSpan.FromDays(2);

    /// <summary>Larger answers are not a profile picture worth keeping.</summary>
    public const int MaxBytes = 512 * 1024;

    // No SVG: an SVG served from the app's own address could run script.
    private static readonly string[] ImageTypes = ["image/png", "image/jpeg", "image/gif", "image/webp"];

    public string Id { get; set; } = "";
    public string? Owner { get; set; }

    /// <summary>The picture's address in Jira (or Gravatar) it was read from; a new one is read at once.</summary>
    public string SourceUrl { get; set; } = "";

    /// <summary>When Jira was asked, whether or not it gave a picture.</summary>
    public DateTime ReadUtc { get; set; }

    /// <summary>The random key in the picture's app URL; empty when Jira gave no picture the app can show.</summary>
    public string Key { get; set; } = "";
    public string? ContentType { get; set; }
    public byte[]? Bytes { get; set; }

    [JsonIgnore]
    public bool HasPicture => Key != "" && Bytes is { Length: > 0 } && ContentType is not null;

    /// <summary>The picture's app URL, relative to the app's base; null when there is no picture.</summary>
    [JsonIgnore]
    public string? Path => HasPicture ? $"profile-picture/{Key}" : null;

    /// <summary>Read from <paramref name="sourceUrl"/> less than <see cref="KeepFor"/> ago: Jira isn't asked again.</summary>
    public bool IsFresh(string sourceUrl, DateTime nowUtc) => SourceUrl == sourceUrl && nowUtc - ReadUtc < KeepFor;

    /// <summary>A plain image of a sensible size.</summary>
    public static bool CanShow(string? contentType, int length) =>
        length is > 0 and <= MaxBytes && contentType is not null && ImageTypes.Contains(contentType.ToLowerInvariant());

    /// <summary>
    /// The address to read for the avatar URL Jira gave; null when there is none or it is Jira's default avatar
    /// (the initials show instead). A Gravatar address is asked for a 404 instead of its stand-in picture.
    /// </summary>
    public static string? SourceFor(string? avatarUrl)
    {
        if (!Uri.TryCreate(avatarUrl, UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https")) return null;
        if (IsJiraDefault(url)) return null;
        return url.Host.EndsWith("gravatar.com", StringComparison.OrdinalIgnoreCase) ? WithoutStandIn(url) : url.AbsoluteUri;
    }

    /// <summary>
    /// Server/DC's own avatars are /secure/useravatar, with an ownerId only when the user uploaded one; Cloud's
    /// stand-ins are the initials and default pictures of its avatar service.
    /// </summary>
    private static bool IsJiraDefault(Uri url)
    {
        var path = url.AbsolutePath.ToLowerInvariant();
        if (path.EndsWith("/secure/useravatar")) return !url.Query.Contains("ownerId=", StringComparison.OrdinalIgnoreCase);
        if (path.EndsWith("/images/icons/avatars/default.png") || path.Contains("/useravatar/default")) return true;
        return url.Host.StartsWith("avatar-management", StringComparison.OrdinalIgnoreCase)
            && (path.Contains("/initials/") || path.Contains("/default"));
    }

    /// <summary>Gravatar answers a user without a picture with the picture named in d=; d=404 makes that a 404.</summary>
    private static string WithoutStandIn(Uri url)
    {
        var kept = url.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !p.StartsWith("d=", StringComparison.OrdinalIgnoreCase) && !p.StartsWith("default=", StringComparison.OrdinalIgnoreCase));
        return new UriBuilder(url) { Query = string.Join('&', kept.Append("d=404")) }.Uri.AbsoluteUri;
    }
}
