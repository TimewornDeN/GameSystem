using GameSystem.Core.Management;
using GameSystem.Core.Models;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.Core;

public class CharacterCatalogTests
{
    private static RemoteCharacterInfo Info(string id, string name, int rarity = 5)
        => new(id, name, "Title of " + name, "Geo", "Sword", rarity, "About " + name, []);

    private static (CharacterCatalog Catalog, FakeGenshinApiClient Api, InMemoryCharacterCatalogStore Store) Create(params RemoteCharacterInfo[] onServer)
    {
        var api = new FakeGenshinApiClient();
        foreach (var info in onServer)
            api.AddCharacter(info);

        var store = new InMemoryCharacterCatalogStore();
        return (new CharacterCatalog(api, store), api, store);
    }

    [Fact]
    public async Task RefreshAsync_DownloadsEveryCharacter_SortsThemByName_AndSavesThem()
    {
        var (catalog, _, store) = Create(Info("zhongli", "Zhongli"), Info("albedo", "Albedo"), Info("keqing", "Keqing"));

        var result = await catalog.RefreshAsync();

        Assert.Equal(["Albedo", "Keqing", "Zhongli"], result.Characters.Select(c => c.Name));
        Assert.Equal(0, result.Failed);
        Assert.Equal(["Albedo", "Keqing", "Zhongli"], store.Items.Select(c => c.Name));
        Assert.Equal(["Albedo", "Keqing", "Zhongli"], catalog.Cached().Select(c => c.Name));
    }

    [Fact]
    public async Task RefreshAsync_WhenNothingCanBeDownloaded_KeepsTheSavedCatalog()
    {
        var (catalog, api, store) = Create(Info("albedo", "Albedo"));
        await catalog.RefreshAsync();
        Assert.Single(store.Items);

        // The server now lists a character it cannot describe (the fake throws for an unknown id)...
        var failing = new CharacterCatalog(new ListOnlyApi(["ghost"], api), store);
        var result = await failing.RefreshAsync();

        Assert.Empty(result.Characters);
        Assert.Equal(1, result.Failed);
        Assert.Equal(["Albedo"], store.Items.Select(c => c.Name));
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task RefreshAsync_SkipsACharacterThatFails_AndCountsIt()
    {
        var (_, api, store) = Create(Info("albedo", "Albedo"), Info("keqing", "Keqing"));
        var catalog = new CharacterCatalog(new ListOnlyApi(["albedo", "ghost", "keqing"], api), store);

        var result = await catalog.RefreshAsync();

        Assert.Equal(["Albedo", "Keqing"], result.Characters.Select(c => c.Name));
        Assert.Equal(1, result.Failed);
        Assert.Equal(2, store.Items.Count);
    }

    [Fact]
    public void Find_MatchesTheIdOrTheName_IgnoringCase()
    {
        var list = new[] { Info("albedo", "Albedo"), Info("kamisato-ayaka", "Kamisato Ayaka") };

        Assert.Equal("albedo", CharacterCatalog.Find(list, "ALBEDO")?.Id);
        Assert.Equal("kamisato-ayaka", CharacterCatalog.Find(list, "kamisato ayaka")?.Id);
        Assert.Null(CharacterCatalog.Find(list, "nobody"));
        Assert.Null(CharacterCatalog.Find(list, "  "));
    }

    [Fact]
    public void PortraitLink_PointsToTheSameImageThatIsDownloaded()
    {
        Assert.Equal("https://genshin.jmp.blue/characters/albedo/icon-big", RemoteLinks.Portrait("albedo"));
        Assert.Throws<ArgumentException>(() => RemoteLinks.Portrait(" "));
    }

    /// <summary>A server whose list of ids is fixed, while the details come from the fake: unknown ids fail.</summary>
    private sealed class ListOnlyApi(string[] ids, FakeGenshinApiClient details) : GameSystem.Core.Abstractions.IGenshinApiClient
    {
        public Task<IReadOnlyList<string>> GetCharacterIdsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>(ids);

        public Task<RemoteCharacterInfo> GetCharacterAsync(string id, CancellationToken ct = default)
            => details.GetCharacterAsync(id, ct);

        public Task<IReadOnlyList<string>> GetWeaponIdsAsync(CancellationToken ct = default)
            => details.GetWeaponIdsAsync(ct);

        public Task<RemoteWeaponInfo> GetWeaponAsync(string id, CancellationToken ct = default)
            => details.GetWeaponAsync(id, ct);
    }
}
