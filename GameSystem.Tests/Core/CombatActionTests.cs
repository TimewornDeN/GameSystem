using GameSystem.Core.Characters;
using GameSystem.Core.Combat.Actions;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.Core;

public class CombatActionTests
{
    private static Warrior NewWarrior() => new("Grosh", new FakeCombatLogger()); // Attack 20, Armor 10, HP 150
    private static Mage NewMage() => new("Elara", new FakeCombatLogger());       // Attack 15, Armor 2, HP 80

    [Fact]
    public void AttackAction_DamagesTarget()
    {
        var warrior = NewWarrior();
        var mage = NewMage();

        new AttackAction(warrior, mage).Execute();

        Assert.Equal(80 - (20 - 2), mage.Health);
    }

    [Fact]
    public void HealAction_RestoresDefaultAmount()
    {
        var mage = NewMage();
        mage.TakeDamage(30); // 28 damage after armor => 52 HP

        new HealAction(mage).Execute();

        Assert.Equal(52 + HealAction.DefaultAmount, mage.Health);
    }

    [Fact]
    public void DefendAction_RaisesDefensiveStance()
    {
        var warrior = NewWarrior();

        new DefendAction(warrior).Execute();

        Assert.True(warrior.IsDefending);
    }

    [Fact]
    public void UseAbilityAction_WithoutId_UsesInnateAbility()
    {
        var warrior = NewWarrior();
        var mage = NewMage();

        new UseAbilityAction(warrior, mage).Execute();

        Assert.Equal(80 - (40 - 2), mage.Health);
    }

    [Fact]
    public void UseAbilityAction_UnknownId_Throws()
    {
        var warrior = NewWarrior();
        var mage = NewMage();

        Assert.Throws<InvalidOperationException>(() => new UseAbilityAction(warrior, mage, "nope").Execute());
    }

    [Fact]
    public void Constructors_RejectNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new AttackAction(null!, NewMage()));
        Assert.Throws<ArgumentNullException>(() => new HealAction(null!));
        Assert.Throws<ArgumentNullException>(() => new DefendAction(null!));
        Assert.Throws<ArgumentNullException>(() => new UseAbilityAction(NewWarrior(), null!));
    }

    [Theory]
    [InlineData("attack", typeof(AttackAction))]
    [InlineData("HEAL", typeof(HealAction))]
    [InlineData("ability", typeof(UseAbilityAction))]
    [InlineData("defend", typeof(DefendAction))]
    public void Factory_CreatesMatchingCommand(string name, Type expectedType)
    {
        var action = CombatActionFactory.Create(name, NewWarrior(), NewMage());

        Assert.NotNull(action);
        Assert.IsType(expectedType, action);
    }

    [Fact]
    public void Factory_Defend_DoesNotNeedTarget()
    {
        Assert.IsType<DefendAction>(CombatActionFactory.Create("defend", NewWarrior(), null));
    }

    [Theory]
    [InlineData("attack")]
    [InlineData("heal")]
    [InlineData("ability")]
    public void Factory_TargetedAction_WithoutTarget_ReturnsNull(string name)
    {
        Assert.Null(CombatActionFactory.Create(name, NewWarrior(), null));
    }

    [Fact]
    public void Factory_UnknownAction_ReturnsNull()
    {
        Assert.Null(CombatActionFactory.Create("dance", NewWarrior(), NewMage()));
    }
}
