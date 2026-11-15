using GameSystem.Core.Characters;
using GameSystem.Core.Combat;
using GameSystem.Core.Models;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.Core;

public class CharacterTests
{
    private static FakeCombatLogger Logger() => new();

    [Fact]
    public void Warrior_HasExpectedBaseStats()
    {
        var warrior = new Warrior("Grosh", Logger());

        Assert.Equal("warrior", warrior.ClassId);
        Assert.Equal(150, warrior.MaxHealth);
        Assert.Equal(150, warrior.Health);
        Assert.Equal(10, warrior.Armor);
        Assert.Equal(20, warrior.AttackPower);
        Assert.Equal("heavy-strike", warrior.DefaultAbilityId);
        Assert.Single(warrior.Abilities);
    }

    [Fact]
    public void Mage_HasExpectedBaseStats()
    {
        var mage = new Mage("Elara", Logger());

        Assert.Equal("mage", mage.ClassId);
        Assert.Equal(80, mage.MaxHealth);
        Assert.Equal(2, mage.Armor);
        Assert.Equal(15, mage.AttackPower);
        Assert.Equal("fireball", mage.DefaultAbilityId);
    }

    [Fact]
    public void Attack_ReducesTargetHealth_ByAttackPowerMinusArmor()
    {
        var attacker = new Warrior("Grosh", Logger());   // AttackPower 20
        var defender = new Mage("Elara", Logger());      // Armor 2

        attacker.Attack(defender);

        Assert.Equal(80 - (20 - 2), defender.Health);
    }

    [Fact]
    public void TakeDamage_NeverDealsLessThanOne()
    {
        var defender = new Warrior("Tank", Logger());    // Armor 10

        defender.TakeDamage(1); // damage << armor

        Assert.Equal(defender.MaxHealth - 1, defender.Health);
    }

    [Fact]
    public void TakeDamage_CannotReduceHealthBelowZero()
    {
        var defender = new Mage("Elara", Logger());

        defender.TakeDamage(10_000);

        Assert.Equal(0, defender.Health);
    }

    [Fact]
    public void Defend_DoublesArmorForTheNextHitOnly()
    {
        var defender = new Warrior("Grosh", Logger());   // Armor 10
        var attacker = new Mage("Elara", Logger());      // AttackPower 15

        defender.Defend();
        Assert.True(defender.IsDefending);

        attacker.Attack(defender); // 15 - (10*2) => clamped to minimum 1 damage
        Assert.Equal(defender.MaxHealth - 1, defender.Health);
        Assert.False(defender.IsDefending); // stance consumed

        int healthBeforeSecondHit = defender.Health;
        attacker.Attack(defender); // normal armor now (10)
        Assert.Equal(healthBeforeSecondHit - (15 - 10), defender.Health);
    }

    [Fact]
    public void Heal_CannotExceedMaxHealth()
    {
        var character = new Warrior("Grosh", Logger());
        character.TakeDamage(10);

        character.Heal(1000);

        Assert.Equal(character.MaxHealth, character.Health);
    }

    [Fact]
    public void Heal_WithNonPositiveAmount_Throws()
    {
        var character = new Warrior("Grosh", Logger());

        Assert.Throws<ArgumentException>(() => character.Heal(0));
    }

    [Fact]
    public void Equip_AddsBonuses_AndIsNotCumulativeAcrossItems()
    {
        var character = new Warrior("Grosh", Logger());
        int baseAttack = character.AttackPower;

        character.Equip(new Item("Dagger", attackBonus: 5));
        character.Equip(new Item("Greatsword", attackBonus: 15)); // replaces the dagger, not stacks

        Assert.Equal(baseAttack + 15, character.AttackPower);
        Assert.Equal("Greatsword", character.EquippedItem!.Name);
    }

    [Fact]
    public void Equip_HealthBonus_IncreasesCurrentHealthToo()
    {
        var character = new Mage("Elara", Logger());
        int baseMax = character.MaxHealth;

        character.Equip(new Item("Amulet", healthBonus: 20));

        Assert.Equal(baseMax + 20, character.MaxHealth);
        Assert.Equal(baseMax + 20, character.Health);
    }

    [Fact]
    public void Learn_DuplicateAbilityId_Throws()
    {
        var character = new Warrior("Grosh", Logger());
        var ability = new Ability(new AbilityModel("heavy-strike", "Copycat", 10));

        Assert.Throws<InvalidOperationException>(() => character.Learn(ability));
    }

    [Fact]
    public void UseAbility_UnknownId_Throws()
    {
        var actor = new Warrior("Grosh", Logger());
        var target = new Mage("Elara", Logger());

        Assert.Throws<InvalidOperationException>(() => actor.UseAbility("does-not-exist", target));
    }

    [Fact]
    public void UseAbility_Default_Warrior_DealsDoubleAttackPower_MinusArmor()
    {
        var warrior = new Warrior("Grosh", Logger());     // AttackPower 20 -> 40 power
        var mage = new Mage("Elara", Logger());           // Armor 2

        warrior.UseAbility(mage);

        Assert.Equal(80 - (40 - 2), mage.Health);
    }

    [Fact]
    public void UseAbility_Default_Mage_IgnoresTargetArmor()
    {
        var mage = new Mage("Elara", Logger());
        var warrior = new Warrior("Grosh", Logger());     // Armor 10

        mage.UseAbility(warrior);

        // Fireball power(35) + target.Armor is pre-added specifically to cancel out TakeDamage's armor subtraction.
        Assert.Equal(150 - 35, warrior.Health);
    }

    [Fact]
    public void Learn_ThenUseAbility_ById_DealsFixedDamage()
    {
        var actor = new Warrior("Grosh", Logger());
        var target = new Mage("Elara", Logger());         // Armor 2
        var customAbility = new Ability(new AbilityModel("shield-bash", "Shield Bash", 25));

        actor.Learn(customAbility);
        actor.UseAbility("shield-bash", target);

        Assert.Equal(80 - (25 - 2), target.Health);
    }

    [Fact]
    public void SetHealth_ClampsToValidRange()
    {
        var character = new Warrior("Grosh", Logger());

        character.SetHealth(-50);
        Assert.Equal(0, character.Health);

        character.SetHealth(999_999);
        Assert.Equal(character.MaxHealth, character.Health);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyName(string name)
    {
        Assert.Throws<ArgumentException>(() => new Warrior(name, Logger()));
    }
}
