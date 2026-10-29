using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using JiraTime.Storage;

namespace JiraTime.Domain;

/// <summary>Who a user is: a Jira host plus the user id that host gave them.</summary>
public sealed record UserKey(string Host, string JiraUserId)
{
    /// <summary>Builds the key from a connection's base URL; scheme, case and trailing slashes don't matter.</summary>
    public static UserKey For(string baseUrl, string jiraUserId) => new(NormalizeHost(baseUrl), jiraUserId);

    /// <summary>
    /// Owner id stored on every document of this user. Derived from host and user id, and safe to use
    /// in file names.
    /// </summary>
    public string Id => Hash($"{Host}|{JiraUserId}");

    /// <summary>"jira.example.com" or "jira.example.com:8443/jira": host, port (when not default) and path, lower case.</summary>
    public static string NormalizeHost(string baseUrl)
    {
        var uri = new Uri(baseUrl.Trim());
        var host = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
        return (host + uri.AbsolutePath.TrimEnd('/')).ToLowerInvariant();
    }

    internal static string Hash(string text) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..24];
}

/// <summary>A document that belongs to one user: only that user may read or change it.</summary>
public interface IOwnedDocument : IDocument
{
    /// <summary><see cref="UserKey.Id"/> of the user. Empty on data from before multi-user support.</summary>
    string? Owner { get; }
}

/// <summary>A user Jira has confirmed at least once (id = <see cref="UserKey.Id"/>).</summary>
[DocumentCollection("users")]
public class UserAccount : IDocument
{
    public string Id { get; set; } = "";
    public string Host { get; set; } = "";
    public string JiraUserId { get; set; } = "";
    public JiraProfile Profile { get; set; } = new("", null, "", null);

    /// <summary>How many confirmed tokens to remember per user (old ones are dropped).</summary>
    public const int RememberedTokens = 5;

    /// <summary>Hashes of tokens Jira confirmed for this user, newest first, so they still work while Jira can't be reached.</summary>
    public List<string> VerifiedTokens { get; set; } = [];

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastVerifiedUtc { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public UserKey Key => new(Host, JiraUserId);

    /// <summary>
    /// Records that Jira just confirmed the token with hash <paramref name="tokenHash"/> for this user: the profile
    /// is Jira's, the token moves to the front and only the latest <see cref="RememberedTokens"/> are kept.
    /// </summary>
    public void Confirm(JiraProfile profile, string tokenHash, DateTime nowUtc)
    {
        Profile = profile;
        LastVerifiedUtc = nowUtc;
        VerifiedTokens.Remove(tokenHash);
        VerifiedTokens.Insert(0, tokenHash);
        if (VerifiedTokens.Count > RememberedTokens) VerifiedTokens.RemoveRange(RememberedTokens, VerifiedTokens.Count - RememberedTokens);
    }

    public static string TokenHash(JiraConnection c) => UserKey.Hash($"{UserKey.NormalizeHost(c.BaseUrl)}|{c.Email}|{c.Token}");
}
