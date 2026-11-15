using GameSystem.Core.Characters;
using GameSystem.Core.Combat;
using GameSystem.Core.Combat.Actions;
using GameSystem.Core.Engine;
using GameSystem.Core.Management;
using GameSystem.Core.Models;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.Core;

/// <summary>Practical 14: the damage formula, resist, buffs and debuffs, speed, turns and the history of a battle.</summary>
public class DamageCalculatorTests
{
    [Theory]
    [InlineData(30, 10, 0, 20)]   // armor only
    [InlineData(30, 10, 50, 10)]  // (30 - 10) * 50%
    [InlineData(5, 10, 0, 1)]     // armor is bigger than the hit: still a scratch
    [InlineData(5, 10, 75, 1)]    // the scratch is never rounded down to zero
    [InlineData(100, 0, 90, 25)]  // resist is capped at 75%
    public void Resolve_AppliesArmorThenResist(int raw, int armor, int resist, int expected)
    {
        Assert.Equal(expected, DamageCalculator.Resolve(new Hit(raw), armor, resist));
    }

    [Fact]
    public void Resolve_IgnoringArmor_SkipsFlatArmor()
    {
        Assert.Equal(30, DamageCalculator.Resolve(new Hit(30, IgnoresArmor: true), armor: 10, resist: 0));
    }

    [Fact]
    public void Resolve_ResistPenetration_LowersTheResistOfTheTarget()
    {
        // resist 50 - penetration 20 = 30, so 70% of 40 gets through.
        Assert.Equal(28, DamageCalculator.Resolve(new Hit(40, IgnoresArmor: true, ResistPenetration: 20), armor: 5, resist: 50));
    }

    [Fact]
    public void Resolve_NegativeDamage_Throws()
    {
        Assert.Throws<ArgumentException>(() => DamageCalculator.Resolve(new Hit(-1), 0, 0));
    }
}

public class CombatMechanicsTests
{
    private static Warrior NewWarrior(string name = "Grosh") => new(name, new FakeCombatLogger()); // Attack 20, Armor 10, HP 150
    private static Mage NewMage(string name = "Elara") => new(name, new FakeCombatLogger());       // Attack 15, Armor 2, HP 80

    [Fact]
    public void ItemResist_ReducesEveryHitAfterArmor()
    {
        var warrior = NewWarrior();
        warrior.Equip(new Item("Cloak", resistBonus: 50));

        NewMage().Attack(warrior); // (15 - 10) = 5, 50% of it gets through => 2

        Assert.Equal(50, warrior.Resist);
        Assert.Equal(150 - 2, warrior.Health);
    }

    [Fact]
    public void ItemSpeed_ChangesSpeedAndIsRemovedWithTheItem()
    {
        var mage = NewMage();

        mage.Equip(new Item("Boots", speedBonus: 25));
        Assert.Equal(Character.BaseSpeed + 25, mage.Speed);

        var memento = mage.CreateMemento();
        mage.Equip(new Item("Plain ring"));
        Assert.Equal(Character.BaseSpeed, mage.Speed);

        mage.Restore(memento);
        Assert.Equal(Character.BaseSpeed + 25, mage.Speed);
    }

    [Fact]
    public void Debuff_LowersEffectiveStat_AndWearsOffAfterItsTurns()
    {
        var warrior = NewWarrior();
        warrior.AddEffect(new StatusEffect("Break", StatKind.Armor, -4, Turns: 2));

        Assert.Equal(6, warrior.EffectiveArmor);
        Assert.Equal(10, warrior.Armor); // the base value does not change

        warrior.TickEffects();
        Assert.Equal(6, warrior.EffectiveArmor);

        warrior.TickEffects();
        Assert.Empty(warrior.ActiveEffects);
        Assert.Equal(10, warrior.EffectiveArmor);
    }

    [Fact]
    public void Buff_RaisesAttackOfBasicAttacksAndAbilities()
    {
        var warrior = NewWarrior();
        var mage = NewMage();
        warrior.AddEffect(new StatusEffect("Rage", StatKind.Attack, 5, Turns: 3));

        warrior.Attack(mage); // 25 - 2 armor

        Assert.Equal(80 - 23, mage.Health);
    }

    [Fact]
    public void SameEffect_IsRefreshedInsteadOfStacked()
    {
        var warrior = NewWarrior();

        warrior.AddEffect(new StatusEffect("Break", StatKind.Armor, -3, Turns: 1));
        warrior.AddEffect(new StatusEffect("Break", StatKind.Armor, -3, Turns: 4));

        var effect = Assert.Single(warrior.ActiveEffects);
        Assert.Equal(4, effect.Turns);
        Assert.Equal(7, warrior.EffectiveArmor);
    }

    [Fact]
    public void AddEffect_WithoutDuration_Throws()
    {
        Assert.Throws<ArgumentException>(() => NewWarrior().AddEffect(new StatusEffect("Dud", StatKind.Armor, -1, Turns: 0)));
    }

    [Fact]
    public void HeavyStrike_BreaksTheTargetsArmor()
    {
        var warrior = NewWarrior();
        var mage = NewMage();

        warrior.UseAbility(mage);

        Assert.Contains(mage.ActiveEffects, e => e.Name == "Armor Break");
        Assert.Equal(0, mage.EffectiveArmor); // 2 - 3 never goes below zero
    }

    [Fact]
    public void Fireball_IgnoresResistPartially_AndEvenTheDefendStance()
    {
        var warrior = NewWarrior();
        warrior.Equip(new Item("Cloak", resistBonus: 50));
        warrior.Defend();

        NewMage().UseAbility(warrior); // 35, armor ignored, resist 50 - 20 = 30 => 70% => 24

        Assert.Equal(150 - 24, warrior.Health);
        Assert.Contains(warrior.ActiveEffects, e => e.Name == "Scorched");
    }

    [Fact]
    public void KillingBlow_DoesNotLeaveEffectsOnTheDeadTarget()
    {
        var warrior = NewWarrior();
        var mage = NewMage();
        mage.SetHealth(1);

        warrior.UseAbility(mage);

        Assert.Equal(0, mage.Health);
        Assert.Empty(mage.ActiveEffects);
    }

    [Fact]
    public void Memento_RestoresActiveEffects()
    {
        var warrior = NewWarrior();
        warrior.AddEffect(new StatusEffect("Rage", StatKind.Attack, 5, Turns: 3));
        var memento = warrior.CreateMemento();

        warrior.ClearEffects();
        warrior.Restore(memento);

        Assert.Equal(25, warrior.EffectiveAttack);
    }

    [Fact]
    public void Snapshot_KeepsResistAndSpeedOfTheEquippedItem()
    {
        var logger = new FakeCombatLogger();
        var item = new Item("Cape", resistBonus: 20, speedBonus: 10);
        var hero = new Mage("Zed", logger);
        hero.Equip(item);

        var restored = GameStateSnapshotter.Restore(GameStateSnapshotter.Capture([item], [], [hero]), logger);

        var copy = Assert.Single(restored.Characters);
        Assert.Equal(20, copy.Resist);
        Assert.Equal(110, copy.Speed);
    }

    [Fact]
    public void Item_RejectsResistOutsideTheAllowedRange()
    {
        Assert.Throws<ArgumentException>(() => new Item(new ItemModel("Bad", ResistBonus: -1)));
        Assert.Throws<ArgumentException>(() => new Item(new ItemModel("Bad", ResistBonus: 76)));
        Assert.Throws<ArgumentException>(() => new Item(new ItemModel("Bad", SpeedBonus: -51)));
    }
}

public class TurnBasedGameTests
{
    private static Character Warrior(string name = "A") => CharacterFactory.Create("warrior", name, new FakeCombatLogger());
    private static Character Mage(string name = "B") => CharacterFactory.Create("mage", name, new FakeCombatLogger());

    private static void Defend(BattleSession session) => session.Perform(session.CreateAction("defend")!);

    [Fact]
    public void EqualSpeed_KeepsTheGivenOrder()
    {
        var a = Warrior();
        var b = Mage();

        var session = new BattleSession(a, b);

        Assert.Equal([a, b], session.CurrentTurn.Order);
    }

    [Fact]
    public void FasterFighter_ActsFirst()
    {
        var a = Warrior();
        var b = Mage();
        b.Equip(new Item("Boots", speedBonus: 50)); // 150 against 100

        var session = new BattleSession(a, b);

        Assert.Equal([b, a], session.CurrentTurn.Order);
        Assert.Same(b, session.CurrentActor);
    }

    [Fact]
    public void DoubleSpeed_GivesAnExtraActionInTheMiddleOfTheTurn()
    {
        var a = Warrior();
        var b = Mage();
        a.Equip(new Item("Boots", speedBonus: 100)); // 200 against 100

        var session = new BattleSession(a, b);

        Assert.Equal([a, b, a], session.CurrentTurn.Order);

        Defend(session);
        Defend(session);
        Assert.Same(a, session.CurrentActor);
        Assert.Equal(1, session.Round);

        Defend(session);
        Assert.Equal(2, session.Round);
        Assert.Equal(3, session.TurnsPlayed);
    }

    [Fact]
    public void ConfigStartsTheSameBattleAsTheConstructor()
    {
        var a = Warrior();
        var b = Mage();

        var session = new BattleSession(new GameConfig([a, b]));

        Assert.Same(a, session.First);
        Assert.Same(b, session.Second);
        Assert.Equal(1, session.Round);
    }

    [Fact]
    public void Config_RequiresExactlyTwoFighters()
    {
        Assert.Throws<ArgumentException>(() => new GameConfig([Warrior()]));
        Assert.Throws<ArgumentException>(() => new GameConfig([Warrior("A"), Mage("B"), Mage("C")]));
        Assert.Throws<ArgumentNullException>(() => new BattleSession((GameConfig)null!));
    }

    [Fact]
    public void History_RecordsEveryActionWithTheHealthChange()
    {
        var a = Warrior();
        var b = Mage();
        var session = new BattleSession(a, b);

        session.Perform(session.CreateAction("attack")!); // 20 - 2 armor
        Defend(session);

        Assert.Equal(2, session.History.Count);

        var first = session.History[0];
        Assert.Equal(1, first.Sequence);
        Assert.Equal(1, first.Round);
        Assert.Equal("A", first.Actor);
        Assert.Equal("attack", first.Action);
        Assert.Equal(18, first.DamageDealt);
        Assert.Equal(80, first.OpponentHealthBefore);
        Assert.Equal(62, first.OpponentHealthAfter);

        Assert.Equal("defend", session.History[1].Action);
        Assert.Equal(0, session.History[1].DamageDealt);
    }

    [Fact]
    public void History_RecordsHealingAndTheKillingBlow()
    {
        var a = Warrior();
        var b = Mage();
        a.SetHealth(100);
        b.SetHealth(1);
        var session = new BattleSession(a, b);

        session.Perform(session.CreateAction("heal")!);
        Assert.Equal(20, session.History[0].HealthRestored);

        Defend(session); // B defends, then A kills B
        session.Perform(session.CreateAction("attack")!);

        Assert.True(session.IsFinished);
        Assert.Equal(1, session.History[^1].DamageDealt);
        Assert.Equal(3, session.History.Count);
    }

    [Fact]
    public void FailedAction_IsNotRecordedAndKeepsTheTurn()
    {
        var a = Warrior();
        var session = new BattleSession(a, Mage());

        Assert.ThrowsAny<Exception>(() => session.Perform(session.CreateAction("ability", "nope")!));

        Assert.Empty(session.History);
        Assert.Equal(0, session.TurnsPlayed);
        Assert.Same(a, session.CurrentActor);
    }

    [Fact]
    public void Effects_WearOffAsTurnsPass_AndDoNotSurviveIntoANewBattle()
    {
        var a = Warrior();
        var b = Mage();
        var session = new BattleSession(a, b);

        session.Perform(session.CreateAction("ability")!); // Armor Break on B for 2 turns
        Assert.Single(b.ActiveEffects);

        Defend(session); // turn 2 begins: 1 turn left
        Assert.Equal(1, Assert.Single(b.ActiveEffects).Turns);

        Defend(session);
        Defend(session); // turn 3 begins: the effect is gone
        Assert.Empty(b.ActiveEffects);

        b.AddEffect(new StatusEffect("Old", StatKind.Armor, -1, Turns: 5));
        _ = new BattleSession(a, b);
        Assert.Empty(b.ActiveEffects);
    }

    [Fact]
    public void SpeedBuff_ChangesTheOrderOfTheNextTurn()
    {
        var a = Warrior();
        var b = Mage();
        var session = new BattleSession(a, b);

        b.AddEffect(new StatusEffect("Haste", StatKind.Speed, 60, Turns: 3));
        Defend(session);
        Defend(session); // turn 2 is built with the buff already active

        Assert.Equal([b, a], session.CurrentTurn.Order);
    }

    [Fact]
    public void Turn_CannotActBeforeItBegins()
    {
        var turn = new Turn(1, [Warrior(), Mage()]);

        Assert.Throws<InvalidOperationException>(() => turn.Act(new DefendAction(Warrior()), 1));
    }
}

public class CharacterProfileLoaderTests
{
    [Fact]
    public void Build_CreatesTheCharacterWithItsItemAndAbilities()
    {
        var profile = new CharacterProfile(
            "mage", "Zed",
            Item: new ItemModel("Cape", ResistBonus: 20),
            Abilities: [new AbilityModel("bolt", "Bolt", 30, IgnoresArmor: true)]);

        var loaded = CharacterProfileLoader.Build(profile, new FakeCombatLogger());

        Assert.Equal("Zed", loaded.Character.Name);
        Assert.Equal("mage", loaded.Character.ClassId);
        Assert.Equal(20, loaded.Character.Resist);
        Assert.Equal("Cape", loaded.Item!.Name);
        Assert.Contains(loaded.Character.Abilities, a => a.Id == "bolt");
        Assert.Single(loaded.Abilities);
    }

    [Fact]
    public void Build_WithoutItemAndAbilities_GivesABareCharacter()
    {
        var loaded = CharacterProfileLoader.Build(new CharacterProfile("warrior", "Bob"), new FakeCombatLogger());

        Assert.Null(loaded.Item);
        Assert.Empty(loaded.Abilities);
        Assert.Null(loaded.Character.EquippedItem);
    }

    [Fact]
    public void Build_UnknownClass_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CharacterProfileLoader.Build(new CharacterProfile("necromancer", "Bob"), new FakeCombatLogger()));
    }

    [Fact]
    public void Build_DuplicateOfTheInnateAbility_Throws()
    {
        var profile = new CharacterProfile("warrior", "Bob", Abilities: [new AbilityModel("heavy-strike", "Copy", 5)]);

        Assert.Throws<InvalidOperationException>(() => CharacterProfileLoader.Build(profile, new FakeCombatLogger()));
    }
}
