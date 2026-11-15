using GameSystem.Core.Characters;
using GameSystem.Core.Combat;
using GameSystem.Core.Management;
using GameSystem.Core.Models;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.Core;

public class CharacterMementoTests
{
    [Fact]
    public void Restore_RevertsEquipmentHealthAndAbilitiesToTheSnapshot()
    {
        var warrior = new Warrior("Grosh", new FakeCombatLogger());
        warrior.Equip(new Item("Sword", attackBonus: 5));
        warrior.Learn(new Ability(new AbilityModel("bash", "Bash", 25)));
        warrior.TakeDamage(30); // 20 damage after armor => 130 HP

        var memento = warrior.CreateMemento();

        warrior.Equip(new Item("Axe", healthBonus: 10, attackBonus: 20));
        warrior.TakeDamage(100);
        warrior.Restore(memento);

        Assert.Equal(130, warrior.Health);
        Assert.Equal(150, warrior.MaxHealth);
        Assert.Equal(25, warrior.AttackPower);
        Assert.Equal("Sword", warrior.EquippedItem!.Name);
        Assert.Contains(warrior.Abilities, a => a.Id == "bash");
    }

    [Fact]
    public void Restore_RemovesAbilitiesLearnedAfterTheSnapshot()
    {
        var warrior = new Warrior("Grosh", new FakeCombatLogger());
        var memento = warrior.CreateMemento();

        warrior.Learn(new Ability(new AbilityModel("bash", "Bash", 25)));
        warrior.Restore(memento);

        Assert.Single(warrior.Abilities);
    }

    [Fact]
    public void Restore_WithNoItemInSnapshot_UnequipsCurrentItem()
    {
        var warrior = new Warrior("Grosh", new FakeCombatLogger());
        var memento = warrior.CreateMemento();

        warrior.Equip(new Item("Sword", healthBonus: 10, attackBonus: 5));
        warrior.Restore(memento);

        Assert.Null(warrior.EquippedItem);
        Assert.Equal(20, warrior.AttackPower);
        Assert.Equal(150, warrior.MaxHealth);
    }

    [Fact]
    public void Restore_MementoOfAnotherCharacter_Throws()
    {
        var logger = new FakeCombatLogger();
        var mageMemento = new Mage("Elara", logger).CreateMemento();

        Assert.Throws<ArgumentException>(() => new Warrior("Grosh", logger).Restore(mageMemento));
    }
}

public class GameStateSnapshotterTests
{
    [Fact]
    public void CaptureThenRestore_RoundTripsTheWorld()
    {
        var logger = new FakeCombatLogger();
        var sword = new Item("Sword", attackBonus: 5);
        var bash = new Ability(new AbilityModel("bash", "Bash", 25));
        var hero = new Warrior("Grosh", logger);
        hero.Equip(sword);
        hero.Learn(bash);
        hero.TakeDamage(30);

        var state = GameStateSnapshotter.Capture([sword], [bash], [hero]);
        var restored = GameStateSnapshotter.Restore(state, logger);

        var copy = Assert.Single(restored.Characters);
        Assert.Equal(hero.Name, copy.Name);
        Assert.Equal(hero.Health, copy.Health);
        Assert.Equal("Sword", copy.EquippedItem!.Name);
        Assert.Contains(copy.Abilities, a => a.Id == "bash");
        Assert.Single(restored.Items);
        Assert.Single(restored.Abilities);
    }

    [Fact]
    public void Restore_SkipsReferencesToMissingItemsAndAbilities()
    {
        var state = new GameStateModel(
            Items: [],
            Abilities: [],
            Characters: [new CharacterStateModel("warrior", "Grosh", 100, "ghost-item", ["ghost-ability"])]);

        var restored = GameStateSnapshotter.Restore(state, new FakeCombatLogger());

        var hero = Assert.Single(restored.Characters);
        Assert.Equal(100, hero.Health);
        Assert.Null(hero.EquippedItem);
        Assert.Single(hero.Abilities);
    }

    [Fact]
    public void Restore_UnknownClass_Throws()
    {
        var state = new GameStateModel([], [], [new CharacterStateModel("necromancer", "Bob", 10, null, [])]);

        Assert.Throws<ArgumentException>(() => GameStateSnapshotter.Restore(state, new FakeCombatLogger()));
    }
}
