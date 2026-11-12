using GameSystem.Core.Characters;
using GameSystem.Core.Combat;
using GameSystem.Core.Management;
using GameSystem.Core.Models;
using GameSystem.Infra.Rendering;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.Core;

public class CharacterSheetBuilderTests
{
    [Fact]
    public void Build_WithoutRemoteInfo_RendersLocalStatsAndInnateAbility()
    {
        var warrior = new Warrior("Grosh", new FakeCombatLogger());

        var sheet = TextRenderer.Render(CharacterSheetBuilder.Build(warrior));

        Assert.Contains("Grosh", sheet);
        Assert.Contains("Class: warrior", sheet);
        Assert.Contains("HP: 150/150", sheet);
        Assert.Contains("Equipped: (nothing)", sheet);
        Assert.Contains("heavy-strike: Heavy Strike", sheet);
    }

    [Fact]
    public void Build_WithRemoteInfo_IncludesTitleElementRarityAndPortraitLink()
    {
        var mage = new Mage("Elara", new FakeCombatLogger());
        var info = new RemoteCharacterInfo(
            Id: "elara",
            Name: "Elara",
            Title: "The Wise",
            Element: "Cryo",
            WeaponType: "Catalyst",
            Rarity: 5,
            Description: "A powerful spellcaster.",
            Abilities: []);

        var sheet = TextRenderer.Render(CharacterSheetBuilder.Build(mage, info));

        Assert.Contains("Elara \u2014 The Wise", sheet);
        Assert.Contains("Element: Cryo", sheet);
        Assert.Contains("5\u2605", sheet);
        Assert.Contains("A powerful spellcaster.", sheet);
        Assert.Contains("[Portrait](https://genshin.jmp.blue/characters/elara/portrait)", sheet);
    }

    [Fact]
    public void Build_WithEquippedItem_ShowsItsName()
    {
        var warrior = new Warrior("Grosh", new FakeCombatLogger());
        warrior.Equip(new Item("Flaming Sword", attackBonus: 15));

        var sheet = TextRenderer.Render(CharacterSheetBuilder.Build(warrior));

        Assert.Contains("Equipped: Flaming Sword", sheet);
    }

    [Fact]
    public void Build_MissingDescription_FallsBackToPlaceholder()
    {
        var warrior = new Warrior("Grosh", new FakeCombatLogger());
        var info = new RemoteCharacterInfo("id", "Grosh", "Title", "Geo", "Claymore", 4, "", []);

        var sheet = TextRenderer.Render(CharacterSheetBuilder.Build(warrior, info));

        Assert.Contains("(no remote description available)", sheet);
    }
}
