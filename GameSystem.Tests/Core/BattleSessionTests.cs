using GameSystem.Core.Characters;
using GameSystem.Core.Engine;
using GameSystem.Core.Management;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.Core;

public class BattleSessionTests
{
    private static Character Warrior(string name) => CharacterFactory.Create("warrior", name, new FakeCombatLogger());

    private static Character Mage(string name) => CharacterFactory.Create("mage", name, new FakeCombatLogger());

    [Fact]
    public void Constructor_SameCharacterTwice_Throws()
    {
        var a = Warrior("A");

        Assert.Throws<ArgumentException>(() => new BattleSession(a, a));
    }

    [Fact]
    public void Constructor_DefeatedFighter_Throws()
    {
        var a = Warrior("A");
        var b = Mage("B");
        b.SetHealth(0);

        Assert.Throws<ArgumentException>(() => new BattleSession(a, b));
    }

    [Fact]
    public void Fighters_AlternateAndTheRoundGrowsAfterTheSecondOne()
    {
        var a = Warrior("A");
        var b = Mage("B");
        var session = new BattleSession(a, b);

        Assert.Same(a, session.CurrentActor);
        Assert.Equal(1, session.Round);

        session.Perform(session.CreateAction("defend")!);
        Assert.Same(b, session.CurrentActor);
        Assert.Equal(1, session.Round);

        session.Perform(session.CreateAction("defend")!);
        Assert.Same(a, session.CurrentActor);
        Assert.Equal(2, session.Round);
        Assert.Equal(2, session.TurnsPlayed);
    }

    [Fact]
    public void KillingTheOpponent_FinishesTheBattle()
    {
        var a = Warrior("A");
        var b = Mage("B");
        b.SetHealth(1);
        var session = new BattleSession(a, b);

        session.Perform(session.CreateAction("attack")!);

        Assert.True(session.IsFinished);
        Assert.Same(a, session.Winner);
        Assert.Same(b, session.Loser);
        Assert.Equal(1, session.Round);
    }

    [Fact]
    public void Perform_AfterTheEnd_Throws()
    {
        var a = Warrior("A");
        var b = Mage("B");
        b.SetHealth(1);
        var session = new BattleSession(a, b);
        session.Perform(session.CreateAction("attack")!);

        Assert.Throws<InvalidOperationException>(() => session.Perform(session.CreateAction("defend")!));
    }

    [Fact]
    public void Surrender_GivesTheVictoryToTheOpponent()
    {
        var a = Warrior("A");
        var b = Mage("B");
        var session = new BattleSession(a, b);

        session.Surrender();

        Assert.True(session.IsFinished);
        Assert.Same(b, session.Winner);
        Assert.Same(a, session.Loser);
    }

    [Fact]
    public void Heal_TargetsTheCurrentActor()
    {
        var a = Warrior("A");
        var b = Mage("B");
        a.SetHealth(50);
        var session = new BattleSession(a, b);

        session.Perform(session.CreateAction("heal")!);

        Assert.Equal(70, a.Health);
    }

    [Fact]
    public void CreateAction_UnknownName_ReturnsNull()
    {
        var session = new BattleSession(Warrior("A"), Mage("B"));

        Assert.Null(session.CreateAction("dance"));
    }

    [Fact]
    public void Perform_FailingAction_KeepsTheTurn()
    {
        var a = Warrior("A");
        var session = new BattleSession(a, Mage("B"));
        var action = session.CreateAction("ability", "does-not-exist")!;

        Assert.ThrowsAny<Exception>(() => session.Perform(action));

        Assert.Same(a, session.CurrentActor);
        Assert.Equal(0, session.TurnsPlayed);
    }

    [Fact]
    public void ToResult_BeforeTheEnd_Throws()
    {
        var session = new BattleSession(Warrior("A"), Mage("B"));

        Assert.Throws<InvalidOperationException>(() => session.ToResult(DateTime.UtcNow));
    }

    [Fact]
    public void ToResult_DescribesTheOutcome()
    {
        var a = Warrior("A");
        var b = Mage("B");
        var session = new BattleSession(a, b);
        session.Surrender();
        var now = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        var result = session.ToResult(now);

        Assert.Equal("B", result.Winner);
        Assert.Equal("A", result.Loser);
        Assert.Equal(b.Health, result.WinnerHealth);
        Assert.Equal(b.MaxHealth, result.WinnerMaxHealth);
        Assert.Equal(now, result.FinishedAtUtc);
    }
}
