using GameSystem.Core.Characters;
using GameSystem.Core.Combat;
using GameSystem.Core.Combat.Strategies;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.Core;

public class DamageStrategyTests
{
    private static Warrior NewWarrior() => new("Grosh", new FakeCombatLogger()); // Attack 20, Armor 10
    private static Mage NewMage() => new("Elara", new FakeCombatLogger());       // Attack 15, Armor 2

    [Fact]
    public void FixedDamage_ReturnsConstantPower()
    {
        var strategy = new FixedDamageStrategy(30);

        Assert.Equal(30, strategy.Compute(NewWarrior(), NewMage()));
        Assert.False(strategy.IgnoresArmor);
    }

    [Fact]
    public void FixedDamage_IgnoringArmor_AddsTargetArmor()
    {
        var strategy = new FixedDamageStrategy(30, ignoresArmor: true);

        Assert.Equal(40, strategy.Compute(NewMage(), NewWarrior())); // 30 + warrior armor 10
        Assert.True(strategy.IgnoresArmor);
    }

    [Fact]
    public void AttackScaling_FollowsCurrentAttackPower()
    {
        var warrior = NewWarrior();
        var strategy = new AttackScalingDamageStrategy(multiplier: 2);

        Assert.Equal(40, strategy.Compute(warrior, NewMage()));

        warrior.Equip(new Item("Sword", attackBonus: 5));

        Assert.Equal(50, strategy.Compute(warrior, NewMage()));
    }

    [Fact]
    public void Ability_WithCustomStrategy_DelegatesToIt()
    {
        var ability = new Ability("smite", "Smite", new AttackScalingDamageStrategy(3));

        Assert.Equal(60, ability.ComputeDamage(NewWarrior(), NewMage()));
        Assert.Equal(0, ability.ToModel().Power); // Not a fixed-power ability, so nothing meaningful to persist.
    }
}
