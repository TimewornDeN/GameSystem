using GameSystem.Core.Abstractions;
using GameSystem.Core.Models;
using GameSystem.Presentation.Catalog;
using Xunit;

namespace GameSystem.Tests.Gui;

public class CatalogViewModelTests
{
    private sealed class FakePortraits(params string[] known) : IPortraitProvider
    {
        public List<string> Requested { get; } = [];

        public Task<byte[]?> GetPortraitAsync(string remoteId, CancellationToken ct = default)
        {
            Requested.Add(remoteId);
            return Task.FromResult<byte[]?>(known.Contains(remoteId) ? [1, 2, 3] : null);
        }
    }

    private static CharacterCard Card(string name, string? remoteId = null, int health = 100)
        => new(name, "warrior", health, 100, 20, 10, 0, 100, remoteId, "", "", "", 0, "", 1, false);

    private static CatalogViewModel Create(IPortraitProvider? portraits) => new(portraits, action => action());

    [Fact]
    public void Update_ShowsOneViewPerCard_AndKnowsWhenItIsEmpty()
    {
        var catalog = Create(null);
        Assert.True(catalog.IsEmpty);

        catalog.Update([Card("Grosh"), Card("Elara")]);

        Assert.Equal(["Grosh", "Elara"], catalog.Cards.Select(c => c.Card.Name));
        Assert.False(catalog.IsEmpty);
    }

    [Fact]
    public void Update_WithTheSameCharacters_UpdatesTheViewsInPlace()
    {
        var catalog = Create(null);
        catalog.Update([Card("Grosh")]);
        var view = catalog.Cards[0];

        catalog.Update([Card("Grosh", health: 40)]);

        Assert.Same(view, catalog.Cards[0]);
        Assert.Equal(40, view.Card.Health);
    }

    [Fact]
    public void Update_LoadsThePortraitOfARemoteCharacter()
    {
        var portraits = new FakePortraits("albedo");
        var catalog = Create(portraits);

        catalog.Update([Card("Albi", "albedo"), Card("Grosh")]);

        Assert.Equal([1, 2, 3], catalog.Cards[0].PortraitBytes);
        Assert.True(catalog.Cards[0].HasPortrait);
        Assert.Null(catalog.Cards[1].PortraitBytes);
        Assert.Equal(["albedo"], portraits.Requested);
    }

    [Fact]
    public void Update_ALostPortrait_LeavesThePlaceholder()
    {
        var catalog = Create(new FakePortraits());

        catalog.Update([Card("Albi", "albedo")]);

        Assert.Null(catalog.Cards[0].PortraitBytes);
        Assert.False(catalog.Cards[0].HasPortrait);
    }

    [Fact]
    public void Update_ReusesAPortraitThatWasAlreadyLoaded()
    {
        var portraits = new FakePortraits("albedo");
        var catalog = Create(portraits);
        catalog.Update([Card("Albi", "albedo")]);

        catalog.Update([Card("Albi", "albedo"), Card("Grosh")]);

        Assert.Equal([1, 2, 3], catalog.Cards[0].PortraitBytes);
        Assert.Single(portraits.Requested);
    }

    [Fact]
    public void Update_TwoCharactersFromTheSameRemoteId_BothGetThePicture()
    {
        var catalog = Create(new FakePortraits("albedo"));

        catalog.Update([Card("One", "albedo"), Card("Two", "albedo")]);

        Assert.All(catalog.Cards, c => Assert.True(c.HasPortrait));
    }

    private static ServerCharacterCard ServerCard(string id, string name) => new(id, name, "", "", "", 5, 0);

    [Fact]
    public void UpdateServer_ShowsOneViewPerCharacterOfTheServer_AndLoadsTheirPortraits()
    {
        var catalog = Create(new FakePortraits("albedo"));
        Assert.True(catalog.ServerIsEmpty);

        catalog.UpdateServer([ServerCard("albedo", "Albedo"), ServerCard("amber", "Amber")]);

        Assert.Equal(["Albedo", "Amber"], catalog.ServerCards.Select(c => c.Card.Name));
        Assert.False(catalog.ServerIsEmpty);
        Assert.True(catalog.ServerCards[0].HasPortrait);
        Assert.False(catalog.ServerCards[1].HasPortrait);
    }

    [Fact]
    public void UpdateServer_WithTheSameCharacters_LeavesTheListAlone()
    {
        var catalog = Create(null);
        catalog.UpdateServer([ServerCard("albedo", "Albedo")]);
        var view = catalog.ServerCards[0];

        catalog.UpdateServer([ServerCard("albedo", "Albedo")]);

        Assert.Same(view, catalog.ServerCards[0]);
    }

    [Fact]
    public void UpdateServer_TakesThePortraitOfACharacterThatIsAlreadyInTheGame_WithoutAskingAgain()
    {
        var portraits = new FakePortraits("albedo");
        var catalog = Create(portraits);
        catalog.Update([Card("Albi", "albedo")]);

        catalog.UpdateServer([ServerCard("albedo", "Albedo")]);

        Assert.True(catalog.ServerCards[0].HasPortrait);
        Assert.Single(portraits.Requested);
    }

    [Fact]
    public void ServerCharacterCard_TakesItsShortInfoFromTheServerResponse()
    {
        var info = new RemoteCharacterInfo("albedo", "Albedo", "Kreideprinz", "Geo", "Sword", 5,
            new string('x', 300), [new RemoteAbilityInfo("Favonius Bladework", "Normal attack")]);

        var card = ServerCharacterCard.From(info);

        Assert.Equal("albedo", card.Id);
        Assert.Equal("Geo \u00B7 Sword", card.Subtitle);
        Assert.Equal("\u2605\u2605\u2605\u2605\u2605", card.Stars);
        Assert.Equal("A", card.Initial);
        Assert.Equal(1, card.TalentCount);
        Assert.True(card.Description.Length <= 140);
    }
}
