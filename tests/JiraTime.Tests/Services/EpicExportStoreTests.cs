using JiraTime.Domain;
using JiraTime.Services;
using JiraTime.Tests.Support;

namespace JiraTime.Tests.Services;

/// <summary>Fase, Forordning and Hovedleverance per epic, kept per user.</summary>
public class EpicExportStoreTests : IDisposable
{
    private readonly TimeHarness _h = new();

    public void Dispose() => _h.Dispose();

    private EpicExportStore For(UserKey user) => _h.For(user).ExportValues;

    [Fact]
    public async Task Values_are_saved_and_read_back_per_epic()
    {
        var carl = For(TestUsers.Carl);
        await carl.SaveAsync(new EpicExportValues { EpicKey = "bac-500", Fase = " 13. Tilpasning ", Forordning = "Schengen SIS / 76811", HovedleveranceNr = "", HovedleveranceLink = "https://x/4" });

        var v = (await carl.GetAsync(["BAC-500", "BAC-501"]))["BAC-500"];

        Assert.Equal("13. Tilpasning", v.Fase);
        Assert.Equal("Schengen SIS / 76811", v.Forordning);
        Assert.Null(v.HovedleveranceNr);
        Assert.Equal("https://x/4", v.HovedleveranceLink);
        Assert.Single(await carl.GetAsync(["BAC-500", "BAC-501"]));
    }

    [Fact]
    public async Task Each_user_has_their_own_values()
    {
        await For(TestUsers.Carl).SaveAsync(new EpicExportValues { EpicKey = "BAC-500", Fase = "Carl's" });

        Assert.Empty(await For(TestUsers.Anna).GetAsync(["BAC-500"]));

        await For(TestUsers.Anna).SaveAsync(new EpicExportValues { EpicKey = "BAC-500", Fase = "Anna's" });
        Assert.Equal("Carl's", (await For(TestUsers.Carl).GetAsync(["BAC-500"]))["BAC-500"].Fase);
    }

    [Fact]
    public async Task Clearing_every_value_removes_them()
    {
        var carl = For(TestUsers.Carl);
        await carl.SaveAsync(new EpicExportValues { EpicKey = "BAC-500", Fase = "x" });
        await carl.SaveAsync(new EpicExportValues { EpicKey = "BAC-500", Fase = " " });

        Assert.Empty(await carl.GetAsync(["BAC-500"]));
    }
}
