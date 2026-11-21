using GameSystem.Core.Characters;
using GameSystem.Core.Engine;
using GameSystem.Core.Management;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.Core;

/// <summary>Practical 15: a battle in progress can be captured, restored and continued exactly where it stopped.</summary>
public class BattleStatePersistenceTests
{
    private static Character Warrior(string name) => CharacterFactory.Create("warrior", name, new FakeCombatLogger());

    private static Character Mage(string name) => CharacterFactory.Create("mage", name, new FakeCombatLogger());

    private static RestoredWorld RoundTrip(IEnumerable<Character> characters, ResumableBattle battle)
        => GameStateSnapshotter.Restore(
            GameStateSnapshotter.Capture([], [], characters, battle),
            new FakeCombatLogger());

    [Fact]
    public void RestoredBattle_IsAtTheSamePlace_AndKeepsTheHistory()
    {
        var a = Warrior("A");
        var b = Mage("B");
        var session = new BattleSession(a, b);
        session.Perform(session.CreateAction("attack")!);
        session.Perform(session.CreateAction("defend")!);
        session.Perform(session.CreateAction("attack")!);

        var resumed = RoundTrip([a, b], new ResumableBattle(session, SecondFighterIsAi: true));

        var restored = resumed.Battle!;
        Assert.True(restored.SecondFighterIsAi);
        Assert.Equal(session.Round, restored.Session.Round);
        Assert.Equal(session.TurnsPlayed, restored.Session.TurnsPlayed);
        Assert.Equal(session.CurrentActor.Name, restored.Session.CurrentActor.Name);
        Assert.Equal(session.History, restored.Session.History);
        Assert.False(restored.Session.IsFinished);
    }

    [Fact]
    public void RestoredBattle_UsesTheRestoredCharacters_NotNewOnes()
    {
        var a = Warrior("A");
        var b = Mage("B");
        var session = new BattleSession(a, b);
        session.Perform(session.CreateAction("attack")!);

        var resumed = RoundTrip([a, b], new ResumableBattle(session, false));

        Assert.Contains(resumed.Battle!.Session.First, resumed.Characters);
        Assert.Contains(resumed.Battle.Session.Second, resumed.Characters);
        Assert.Equal(b.Health, resumed.Battle.Session.Second.Health);
    }

    [Fact]
    public void RestoredBattle_PlaysOnExactlyLikeTheOriginal()
    {
        var a = Warrior("A");
        var b = Mage("B");
        var session = new BattleSession(a, b);
        session.Perform(session.CreateAction("attack")!);
        session.Perform(session.CreateAction("defend")!);

        var resumed = RoundTrip([a, b], new ResumableBattle(session, false)).Battle!.Session;

        // B was defending when the game was saved, and it still is after loading.
        Assert.True(resumed.Second.IsDefending);

        foreach (string action in new[] { "attack", "heal", "attack", "defend", "attack" })
        {
            session.Perform(session.CreateAction(action)!);
            resumed.Perform(resumed.CreateAction(action)!);
        }

        Assert.Equal(session.First.Health, resumed.First.Health);
        Assert.Equal(session.Second.Health, resumed.Second.Health);
        Assert.Equal(session.Round, resumed.Round);
        Assert.Equal(session.History, resumed.History);
    }

    [Fact]
    public void ActiveEffects_SurviveTheRoundTrip()
    {
        var a = Warrior("A");
        var b = Mage("B");
        var session = new BattleSession(a, b);
        session.Perform(session.CreateAction("ability")!);
        Assert.NotEmpty(b.ActiveEffects);

        var resumed = RoundTrip([a, b], new ResumableBattle(session, false));

        var restoredB = resumed.Characters.Single(c => c.Name == "B");
        Assert.Equal(b.ActiveEffects, restoredB.ActiveEffects);
        Assert.Equal(b.EffectiveArmor, restoredB.EffectiveArmor);
    }

    [Fact]
    public void RoundOrderWithAFasterFighter_IsKeptAsItWas()
    {
        var a = Warrior("A");
        var b = Mage("B");
        a.Equip(new GameSystem.Core.Combat.Item("Boots", speedBonus: 100)); // A: 200 against 100 gives A, B, A
        var session = new BattleSession(a, b);
        session.Perform(session.CreateAction("defend")!); // A
        session.Perform(session.CreateAction("defend")!); // B, the next one is the extra action of A

        var restored = RoundTrip([a, b], new ResumableBattle(session, false)).Battle!.Session;

        Assert.Equal(1, restored.Round);
        Assert.Equal("A", restored.CurrentActor.Name);
        Assert.Equal(session.CurrentTurn.Order.Select(c => c.Name), restored.CurrentTurn.Order.Select(c => c.Name));

        restored.Perform(restored.CreateAction("defend")!);
        Assert.Equal(2, restored.Round);
    }

    [Fact]
    public void FinishedBattle_IsNotPartOfTheSavedState()
    {
        var a = Warrior("A");
        var b = Mage("B");
        b.SetHealth(1);
        var session = new BattleSession(a, b);
        session.Perform(session.CreateAction("attack")!);
        Assert.True(session.IsFinished);

        var state = GameStateSnapshotter.Capture([], [], [a, b], new ResumableBattle(session, false));

        Assert.Null(state.Battle);
    }

    [Fact]
    public void CaptureState_OfAFinishedBattle_Throws()
    {
        var b = Mage("B");
        b.SetHealth(1);
        var session = new BattleSession(Warrior("A"), b);
        session.Perform(session.CreateAction("attack")!);

        Assert.Throws<InvalidOperationException>(() => session.CaptureState(false));
    }

    [Fact]
    public void WithoutABattle_TheStateHasNoBattle()
    {
        var state = GameStateSnapshotter.Capture([], [], [Warrior("A")]);

        Assert.Null(state.Battle);
        Assert.Null(GameStateSnapshotter.Restore(state, new FakeCombatLogger()).Battle);
    }

    [Fact]
    public void Restore_BattleWithAnUnknownFighter_Throws()
    {
        var a = Warrior("A");
        var b = Mage("B");
        var session = new BattleSession(a, b);
        session.Perform(session.CreateAction("attack")!);
        var state = GameStateSnapshotter.Capture([], [], [a, b], new ResumableBattle(session, false));

        var broken = state with { Battle = state.Battle! with { SecondName = "Nobody" } };

        Assert.Throws<ArgumentException>(() => GameStateSnapshotter.Restore(broken, new FakeCombatLogger()));
    }

    [Fact]
    public void Restore_HistoryThatDoesNotMatchTheCounter_Throws()
    {
        var a = Warrior("A");
        var b = Mage("B");
        var session = new BattleSession(a, b);
        session.Perform(session.CreateAction("attack")!);
        var state = GameStateSnapshotter.Capture([], [], [a, b], new ResumableBattle(session, false));

        var broken = state with { Battle = state.Battle! with { TurnsPlayed = 7 } };

        Assert.Throws<ArgumentException>(() => GameStateSnapshotter.Restore(broken, new FakeCombatLogger()));
    }

    [Fact]
    public void Restore_BattleWithADefeatedFighter_Throws()
    {
        var a = Warrior("A");
        var b = Mage("B");
        var session = new BattleSession(a, b);
        var state = GameStateSnapshotter.Capture([], [], [a, b], new ResumableBattle(session, false));

        var broken = state with
        {
            Characters = [state.Characters[0], state.Characters[1] with { Health = 0 }]
        };

        Assert.Throws<ArgumentException>(() => GameStateSnapshotter.Restore(broken, new FakeCombatLogger()));
    }

    [Fact]
    public void RemoteInfo_IsKeptOnlyForCharactersThatExist()
    {
        var hero = Warrior("Hero");
        var info = new GameSystem.Core.Models.RemoteCharacterInfo("diluc", "Diluc", "Darknight", "Pyro", "Claymore", 5, "text", []);
        var remote = new Dictionary<string, GameSystem.Core.Models.RemoteCharacterInfo>
        {
            ["Hero"] = info,
            ["Ghost"] = info
        };

        var state = GameStateSnapshotter.Capture([], [], [hero], remoteInfo: remote);
        var restored = GameStateSnapshotter.Restore(state, new FakeCombatLogger());

        Assert.Equal("Hero", Assert.Single(state.RemoteInfo!).Key);
        Assert.Equal("Diluc", restored.RemoteInfo!["hero"].Name); // names are matched without regard to case
    }
}
