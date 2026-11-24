using System.Collections.Concurrent;
using GameSystem.CLI;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Models;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.CLI;

/// <summary>The 'catalog' command: the list of all characters of the server, its update, details and portraits.</summary>
public class CatalogCommandTests
{
    private sealed class FakePortraits(params string[] known) : IPortraitProvider
    {
        public ConcurrentBag<string> Requested { get; } = new();

        public Task<byte[]?> GetPortraitAsync(string remoteId, CancellationToken ct = default)
        {
            Requested.Add(remoteId);
            return Task.FromResult<byte[]?>(known.Contains(remoteId) ? [1, 2, 3] : null);
        }

        public string? GetLocalPath(string remoteId)
            => known.Contains(remoteId) ? Path.Combine("portraits", remoteId + ".png") : null;
    }

    private static (Cli Cli, FakeDisplayer Displayer, InMemoryCharacterCatalogStore Store, FakePortraits Portraits) Create(
        bool serverHasCharacters = true,
        params string[] portraitsOnServer)
    {
        var displayer = new FakeDisplayer();
        var api = new FakeGenshinApiClient();

        if (serverHasCharacters)
        {
            api.AddCharacter(new RemoteCharacterInfo("albedo", "Albedo", "Kreideprinz", "Geo", "Sword", 5,
                "Chief alchemist of the Knights of Favonius.", [new RemoteAbilityInfo("Favonius Bladework", "Normal attack")]));
            api.AddCharacter(new RemoteCharacterInfo("amber", "Amber", "Outrider", "Pyro", "Bow", 4, "A scout.", []));
        }

        var store = new InMemoryCharacterCatalogStore();
        var portraits = new FakePortraits(portraitsOnServer);

        var cli = CliFactory.Create(
            displayer,
            new FakeInputReader(),
            new FakeCombatLogger(),
            new InMemoryGameStateRepository(),
            new InMemoryGameResultRepository(),
            api,
            catalogStore: store,
            portraits: portraits);

        return (cli, displayer, store, portraits);
    }

    [Fact]
    public void Catalog_WithNothingSavedYet_DownloadsTheCatalogFirst_AndListsEveryCharacter()
    {
        var (cli, displayer, store, _) = Create();

        cli.ExecCommand("catalog");

        Assert.True(displayer.HasSuccess("Catalog updated: 2 characters"));
        Assert.True(displayer.Any("Albedo"));
        Assert.True(displayer.Any("Amber"));
        Assert.Equal(["Albedo", "Amber"], store.Items.Select(c => c.Name));
    }

    [Fact]
    public void Catalog_WithASavedList_ShowsItWithoutAskingTheServer()
    {
        var (cli, displayer, store, _) = Create();
        store.Items.Add(new RemoteCharacterInfo("zed", "Zed", "", "Electro", "Catalyst", 4, "", []));

        cli.ExecCommand("catalog");

        Assert.True(displayer.Any("Zed"));
        Assert.False(displayer.HasSuccess("Catalog updated"));
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public void CatalogRefresh_AsksTheServerAgain_AndReplacesTheSavedList()
    {
        var (cli, displayer, store, _) = Create();
        store.Items.Add(new RemoteCharacterInfo("old", "Old", "", "", "", 1, "", []));

        cli.ExecCommand("catalog refresh");

        Assert.True(displayer.HasSuccess("Catalog updated"));
        Assert.Equal(["Albedo", "Amber"], store.Items.Select(c => c.Name));
    }

    [Fact]
    public void CatalogRefresh_WhenTheServerReturnsNothing_ReportsAnError_AndKeepsTheSavedList()
    {
        var (cli, displayer, store, _) = Create(serverHasCharacters: false);
        store.Items.Add(new RemoteCharacterInfo("old", "Old", "", "", "", 1, "", []));

        cli.ExecCommand("catalog refresh");

        Assert.True(displayer.HasError("saved catalog was not changed"));
        Assert.Equal(["Old"], store.Items.Select(c => c.Name));
    }

    [Fact]
    public void CatalogInfo_ShowsTheDetails_TheLinkToThePortrait_AndWhereTheFileLies()
    {
        var (cli, displayer, _, portraits) = Create(portraitsOnServer: ["albedo"]);

        cli.ExecCommand("catalog info albedo");

        Assert.True(displayer.Any("Kreideprinz"));
        Assert.True(displayer.Any("Element: Geo"));
        Assert.True(displayer.Any("Favonius Bladework"));
        Assert.True(displayer.Any("https://genshin.jmp.blue/characters/albedo/icon-big"));
        Assert.True(displayer.Any(Path.Combine("portraits", "albedo.png")));
        Assert.Contains("albedo", portraits.Requested);
    }

    [Fact]
    public void CatalogInfo_FindsACharacterByName_AndWarnsWhenThePortraitCannotBeDownloaded()
    {
        var (cli, displayer, _, _) = Create();

        cli.ExecCommand("catalog info amber");

        Assert.True(displayer.Any("Outrider"));
        Assert.True(displayer.Any("https://genshin.jmp.blue/characters/amber/icon-big"));
        Assert.Contains(displayer.Entries, e => e.Kind == DisplayKind.Warning && e.Text.Contains("portrait"));
    }

    [Fact]
    public void CatalogInfo_OfAnUnknownCharacter_ReportsAnError()
    {
        var (cli, displayer, _, _) = Create();

        cli.ExecCommand("catalog info nobody");

        Assert.True(displayer.HasError("not in the catalog"));
    }

    [Fact]
    public void CatalogInfo_WithoutAnId_ShowsTheUsage()
    {
        var (cli, displayer, _, _) = Create();

        cli.ExecCommand("catalog info");

        Assert.True(displayer.HasError("Usage: catalog info"));
    }

    [Fact]
    public void CatalogPortraits_DownloadsThePortraitOfEveryCharacter()
    {
        var (cli, displayer, _, portraits) = Create(portraitsOnServer: ["albedo", "amber"]);

        cli.ExecCommand("catalog portraits");

        Assert.True(displayer.HasSuccess("Portraits on the disk: 2 of 2"));
        Assert.Equal(["albedo", "amber"], portraits.Requested.Order());
    }

    [Fact]
    public void CatalogPortraits_ReportsThePortraitsThatCouldNotBeDownloaded()
    {
        var (cli, displayer, _, _) = Create(portraitsOnServer: ["albedo"]);

        cli.ExecCommand("catalog portraits");

        Assert.True(displayer.HasSuccess("Portraits on the disk: 1 of 2"));
        Assert.Contains(displayer.Entries, e => e.Kind == DisplayKind.Warning && e.Text.Contains("1 portraits could not be downloaded"));
    }

    [Fact]
    public void Catalog_WithAnUnknownSubCommand_ReportsAnError()
    {
        var (cli, displayer, _, _) = Create();

        cli.ExecCommand("catalog bogus");

        Assert.True(displayer.HasError("Unknown catalog sub-command"));
    }

    [Fact]
    public void Catalog_IsListedInHelp()
    {
        var (cli, displayer, _, _) = Create();

        cli.ExecCommand("help");

        Assert.True(displayer.Any("catalog"));
    }
}
