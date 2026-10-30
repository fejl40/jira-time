using JiraTime.Domain;
using JiraTime.Services;

namespace JiraTime.Tests;

/// <summary>An owned collection only ever reads and writes the signed-in user's documents.</summary>
public sealed class OwnedCollectionTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 19);

    private readonly TempStore _files = new();

    public void Dispose() => _files.Dispose();

    private OwnedCollection<DayOff> DaysOff(UserKey user) => new(_files.Store, TestUsers.SignedIn(user));

    private Task StoreAsync(string id, UserKey owner, DateOnly date) =>
        _files.Store.Collection<DayOff>().UpsertAsync(new DayOff { Id = id, Owner = owner.Id, Date = date });

    [Fact]
    public async Task Getting_someone_elses_document_by_id_gives_nothing()
    {
        await StoreAsync("shared-id", TestUsers.Carl, Day);

        Assert.NotNull(await DaysOff(TestUsers.Carl).GetAsync("shared-id"));
        Assert.Null(await DaysOff(TestUsers.Anna).GetAsync("shared-id"));
        Assert.Null(await DaysOff(TestUsers.Carl).GetAsync("missing"));
    }

    [Fact]
    public async Task A_query_never_returns_someone_elses_documents()
    {
        await StoreAsync("carls", TestUsers.Carl, Day);
        await StoreAsync("annas", TestUsers.Anna, Day);
        await StoreAsync("old", TestUsers.Carl, Day.AddDays(-30));

        Assert.Equal(["carls"], (await DaysOff(TestUsers.Carl).QueryAsync(d => d.Date >= Day)).Select(d => d.Id));
        Assert.Equal(["annas"], (await DaysOff(TestUsers.Anna).QueryAsync(_ => true)).Select(d => d.Id));
    }

    [Fact]
    public async Task Saving_stamps_the_signed_in_user_as_owner()
    {
        await DaysOff(TestUsers.Carl).UpsertAsync(new DayOff { Id = "new", Owner = TestUsers.Anna.Id, Date = Day });

        Assert.Equal(TestUsers.Carl.Id, (await _files.Store.Collection<DayOff>().GetAsync("new"))!.Owner);
    }

    [Fact]
    public async Task Saving_over_someone_elses_id_throws_and_changes_nothing()
    {
        await StoreAsync("shared-id", TestUsers.Carl, Day);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DaysOff(TestUsers.Anna).UpsertAsync(new DayOff { Id = "shared-id", Date = Day.AddDays(1) }));

        var stored = await _files.Store.Collection<DayOff>().GetAsync("shared-id");
        Assert.Equal((TestUsers.Carl.Id, Day), (stored!.Owner, stored.Date));
    }

    [Fact]
    public async Task Deleting_someone_elses_document_leaves_it()
    {
        await StoreAsync("shared-id", TestUsers.Carl, Day);

        Assert.False(await DaysOff(TestUsers.Anna).DeleteAsync("shared-id"));
        Assert.True(await DaysOff(TestUsers.Carl).DeleteAsync("shared-id"));
        Assert.Null(await _files.Store.Collection<DayOff>().GetAsync("shared-id"));
    }

    [Fact]
    public async Task Nothing_works_without_a_signed_in_user()
    {
        var days = new OwnedCollection<DayOff>(_files.Store, new CurrentUser());

        await Assert.ThrowsAsync<InvalidOperationException>(() => days.QueryAsync(_ => true));
        await Assert.ThrowsAsync<InvalidOperationException>(() => days.GetAsync("any"));
    }
}
