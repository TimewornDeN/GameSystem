using GameSystem.Core.Characters;
using GameSystem.Core.Combat;
using GameSystem.Core.Models;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.Core;

public class AbilityTests
{
    [Fact]
    public void FixedPowerAbility_ComputesDamage_PlusArmorWhenIgnoringArmor()
    {
        var actor = new Warrior("Grosh", new FakeCombatLogger());
        var target = new Mage("Elara", new FakeCombatLogger()); // Armor 2

        var ability = new Ability(new AbilityModel("snipe", "Snipe", 30, IgnoresArmor: true));

        Assert.Equal(32, ability.ComputeDamage(actor, target));
    }

    [Fact]
    public void FixedPowerAbility_ComputesDamage_WithoutArmorBonus()
    {
        var actor = new Warrior("Grosh", new FakeCombatLogger());
        var target = new Mage("Elara", new FakeCombatLogger());

        var ability = new Ability(new AbilityModel("tap", "Tap", 10));

        Assert.Equal(10, ability.ComputeDamage(actor, target));
    }

    [Fact]
    public void ToModel_PreservesFixedPowerAbilityData()
    {
        var model = new AbilityModel("shield-bash", "Shield Bash", 25, IgnoresArmor: false);
        var ability = new Ability(model);

        Assert.Equal(model, ability.ToModel());
    }

    [Fact]
    public void Constructor_RejectsEmptyId()
    {
        Assert.Throws<ArgumentException>(() => new Ability(new AbilityModel("", "Nameless", 10)));
    }

    [Fact]
    public void MissingDescription_FallsBackToId()
    {
        var ability = new Ability(new AbilityModel("whirlwind", "", 10));

        Assert.Equal("whirlwind", ability.Description);
    }
}
