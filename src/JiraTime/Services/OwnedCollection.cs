using System.Linq.Expressions;
using JiraTime.Domain;
using JiraTime.Storage;
using JiraTime.Services.SignIn;

namespace JiraTime.Services;

/// <summary>
/// One collection of the signed-in user's documents. The only place that checks <see cref="IOwnedDocument.Owner"/>:
/// reads never return someone else's documents and writes never touch them.
/// </summary>
public sealed class OwnedCollection<T>(IDocumentStore store, CurrentUser user) where T : class, IOwnedDocument
{
    private readonly IDocumentCollection<T> _documents = store.Collection<T>();

    /// <summary>The signed-in user's owner id, for building document ids.</summary>
    /// <exception cref="InvalidOperationException">Nobody is signed in.</exception>
    public string Owner => user.Owner;

    /// <summary>The document with this id; null when there is none or it is someone else's.</summary>
    public async Task<T?> GetAsync(string id)
    {
        var owner = Owner;
        return await _documents.GetAsync(id) is { } document && document.Owner == owner ? document : null;
    }

    /// <summary>The user's documents that match <paramref name="predicate"/>; the owner is added to the filter.</summary>
    public Task<IReadOnlyList<T>> QueryAsync(Expression<Func<T, bool>> predicate)
    {
        var owner = Owner;
        Expression<Func<T, bool>> mine = d => d.Owner == owner;
        var document = predicate.Parameters[0];
        var both = Expression.AndAlso(new ParameterSwap(mine.Parameters[0], document).Visit(mine.Body), predicate.Body);
        return _documents.QueryAsync(Expression.Lambda<Func<T, bool>>(both, document));
    }

    /// <summary>Saves the document as the user's.</summary>
    /// <exception cref="InvalidOperationException">The id belongs to someone else's document.</exception>
    public async Task UpsertAsync(T document)
    {
        var owner = Owner;
        if (await _documents.GetAsync(document.Id) is { } stored && stored.Owner != owner)
            throw new InvalidOperationException("This document belongs to someone else.");
        document.Owner = owner;
        await _documents.UpsertAsync(document);
    }

    /// <summary>Deletes the document if it is the user's.</summary>
    /// <returns>False when there was none or it is someone else's.</returns>
    public async Task<bool> DeleteAsync(string id) => await GetAsync(id) is not null && await _documents.DeleteAsync(id);

    /// <summary>Puts one parameter in place of another, so two filters can share the document parameter.</summary>
    private sealed class ParameterSwap(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
    }
}
