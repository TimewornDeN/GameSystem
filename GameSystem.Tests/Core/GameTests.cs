using GameSystem.Core.Characters;
using GameSystem.Core.Combat.Actions;
using GameSystem.Core.Engine;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.Core;

public class GameTests
{
    /// <summary>Scripted strategy: everybody simply attacks, which makes the whole battle deterministic.</summary>
    private sealed class AlwaysAttackStrategy : ITurnStrategy
    {
        public ICombatAction ChooseAction(Character actor, Character opponent)
            => new AttackAction(actor, opponent);
    }

    [Fact]
    public void StartSimulation_UsesInjectedStrategy_AndDeclaresTheWinner()
    {
        var logger = new FakeCombatLogger();
        var warrior = new Warrior("Grosh", logger); // 18 damage per hit against the mage
        var mage = new Mage("Elara", logger);       // only 5 damage per hit against the warrior

        new Game(logger, new AlwaysAttackStrategy()).StartSimulation(warrior, mage);

        Assert.Equal(0, mage.Health);
        Assert.True(warrior.Health > 0);
        Assert.True(logger.Any(m => m.Contains("WINNER: [Grosh]")));
    }

    [Fact]
    public void StartSimulation_WithMissingCombatant_LogsAndReturns()
    {
        var logger = new FakeCombatLogger();

        new Game(logger, new AlwaysAttackStrategy()).StartSimulation(new Warrior("Grosh", logger), null!);

        Assert.True(logger.Any(m => m.Contains("Both combatants must be provided")));
    }

    [Fact]
    public void RandomTurnStrategy_NeverHeals_WhenHealthIsHigh()
    {
        var logger = new FakeCombatLogger();
        var actor = new Warrior("Grosh", logger);
        var opponent = new Mage("Elara", logger);

        for (int seed = 0; seed < 50; seed++)
        {
            var strategy = new RandomTurnStrategy(new Random(seed));

            Assert.IsNotType<HealAction>(strategy.ChooseAction(actor, opponent));
        }
    }

    [Fact]
    public void RandomTurnStrategy_AlwaysReturnsACommand_WhenHealthIsLow()
    {
        var logger = new FakeCombatLogger();
        var actor = new Warrior("Grosh", logger);
        actor.TakeDamage(140); // leaves well under 40% health
        var opponent = new Mage("Elara", logger);

        for (int seed = 0; seed < 50; seed++)
        {
            var strategy = new RandomTurnStrategy(new Random(seed));

            Assert.NotNull(strategy.ChooseAction(actor, opponent));
        }
    }
}
