using System.Linq.Expressions;

namespace JiraTime.Storage;

/// <summary>Anything stored in a document collection. The id is unique within its collection.</summary>
public interface IDocument
{
    string Id { get; set; }
}

/// <summary>Names the collection a document type lives in. Defaults to the type name.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DocumentCollectionAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

/// <summary>
/// Storage entry point. The app only talks to this interface, so the local file
/// implementation can later be swapped for a real document database
/// (MongoDB, Cosmos DB, RavenDB, ...) by registering a different implementation.
/// </summary>
public interface IDocumentStore
{
    IDocumentCollection<T> Collection<T>() where T : class, IDocument;
}

public interface IDocumentCollection<T> where T : class, IDocument
{
    Task<T?> GetAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Expressions (rather than delegates) so a database-backed implementation
    /// can translate the filter into a server-side query.
    /// </summary>
    Task<IReadOnlyList<T>> QueryAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    /// <summary>Insert or replace the whole document.</summary>
    Task UpsertAsync(T document, CancellationToken ct = default);

    /// <returns>False if no document had that id.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}

public static class DocumentCollectionExtensions
{
    public static Task<IReadOnlyList<T>> AllAsync<T>(this IDocumentCollection<T> c, CancellationToken ct = default)
        where T : class, IDocument => c.QueryAsync(_ => true, ct);
}
