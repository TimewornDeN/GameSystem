using GameSystem.Core.Characters;
using GameSystem.Core.Combat;
using GameSystem.Core.Management;
using GameSystem.Core.Models;
using GameSystem.Presentation.Catalog;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.Gui;

public class CharacterCardTests
{
    private static RemoteCharacterInfo Albedo(string description = "The Chief Alchemist.")
        => new("albedo", "Albedo", "Kreideprinz", "Geo", "Sword", 5, description, []);

    [Fact]
    public void From_ALocalCharacter_HasStatsButNoServerInfo()
    {
        var hero = new Warrior("Grosh", new FakeCombatLogger());

        var card = CharacterCard.From(hero, remote: null, isFighting: false);

        Assert.Equal("Grosh", card.Name);
        Assert.Equal("warrior", card.ClassId);
        Assert.Equal("Warrior", card.ClassText);
        Assert.Equal("G", card.Initial);
        Assert.Null(card.RemoteId);
        Assert.Equal(string.Empty, card.Title);
        Assert.Equal(0, card.Rarity);
        Assert.Equal(string.Empty, card.Stars);
        Assert.Equal(100, card.HealthPercent);
    }

    [Fact]
    public void From_ARemoteCharacter_TakesTheShortInfoFromTheServerResponse()
    {
        var hero = new Mage("Albi", new FakeCombatLogger());

        var card = CharacterCard.From(hero, Albedo(), isFighting: true);

        Assert.Equal("albedo", card.RemoteId);
        Assert.Equal("Kreideprinz", card.Title);
        Assert.Equal("Geo \u00B7 Sword", card.Subtitle);
        Assert.Equal(5, card.Rarity);
        Assert.Equal(5, card.Stars.Length);
        Assert.True(card.IsFighting);
    }

    [Fact]
    public void From_ALongDescription_IsShortenedToOneLine()
    {
        var hero = new Mage("Albi", new FakeCombatLogger());
        string longText = "First line.\nSecond line. " + new string('x', 400);

        var card = CharacterCard.From(hero, Albedo(longText), isFighting: false);

        Assert.True(card.Description.Length <= 140);
        Assert.DoesNotContain('\n', card.Description);
        Assert.EndsWith("\u2026", card.Description);
    }

    [Fact]
    public void From_AWoundedCharacter_ShowsTheHealthPercent()
    {
        var hero = new Warrior("Grosh", new FakeCombatLogger());
        hero.SetHealth(hero.MaxHealth / 2);

        var card = CharacterCard.From(hero, null, false);

        Assert.Equal(50, card.HealthPercent, precision: 0);
        Assert.Equal($"{hero.Health}/{hero.MaxHealth} HP", card.HealthText);
    }

    [Fact]
    public void From_ShowsTheEquippedItemAndTheNumberOfAbilities()
    {
        var hero = new Warrior("Grosh", new FakeCombatLogger());
        hero.Equip(new Item("Iron Ring", attackBonus: 3));

        var card = CharacterCard.From(hero, null, false);

        Assert.Equal("Iron Ring", card.ItemName);
        Assert.Equal(hero.Abilities.Count, card.AbilityCount);
        Assert.Equal(hero.EffectiveAttack, card.Attack);
    }

    [Fact]
    public void BuildAll_ReturnsACardForEveryCharacter_WithRemoteInfoWhereItExists()
    {
        var world = new GameWorld();
        var logger = new FakeCombatLogger();
        world.Characters.Add(new Warrior("Grosh", logger));
        world.Characters.Add(new Mage("Albi", logger));
        world.RemoteInfo["Albi"] = Albedo();

        var cards = CharacterCard.BuildAll(world);

        Assert.Equal(["Grosh", "Albi"], cards.Select(c => c.Name));
        Assert.Null(cards[0].RemoteId);
        Assert.Equal("albedo", cards[1].RemoteId);
    }

    [Fact]
    public void BuildAll_MarksTheFightersOfABattleInProgress()
    {
        var world = new GameWorld();
        var logger = new FakeCombatLogger();
        var a = new Warrior("A", logger);
        var b = new Warrior("B", logger);
        world.Characters.AddRange([a, b, new Mage("Idle", logger)]);
        world.Battle = new GameSystem.Core.Engine.ResumableBattle(new GameSystem.Core.Engine.BattleSession(a, b), false);

        var cards = CharacterCard.BuildAll(world);

        Assert.Equal([true, true, false], cards.Select(c => c.IsFighting));
    }
}
