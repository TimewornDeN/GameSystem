using GameSystem.CLI;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.CLI;

/// <summary>
/// Practical 15 through the command line: 'save' during a battle, closing the game, and 'continue' in a new session
/// (a new Cli with an empty world over the same storage, as if the program was started again).
/// </summary>
public class ContinueCommandTests
{
    private static readonly DateTime FixedNow = new(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);

    private sealed class Session
    {
        public Session(InMemoryGameStateRepository states, InMemoryGameResultRepository results, params string?[] inputs)
        {
            Cli = CliFactory.Create(Displayer, new FakeInputReader(inputs), new FakeCombatLogger(), states, results, clock: () => FixedNow);
        }

        public FakeDisplayer Displayer { get; } = new();

        public Cli Cli { get; }
    }

    /// <summary>First run of the program: two characters, Grosh attacks, the game is saved and the input ends.</summary>
    private static (InMemoryGameStateRepository States, InMemoryGameResultRepository Results) FirstRunSavedMidBattle(string slot = "slot1")
    {
        var states = new InMemoryGameStateRepository();
        var results = new InMemoryGameResultRepository();
        var first = new Session(states, results, "Grosh", "1", "Elara", "2", "attack", $"save {slot}");

        first.Cli.ExecCommand("create char");
        first.Cli.ExecCommand("create char");
        first.Cli.ExecCommand("play Grosh Elara");

        return (states, results);
    }

    [Fact]
    public void SaveDuringABattle_StoresTheBattleWithItsHistory()
    {
        var (states, _) = FirstRunSavedMidBattle();

        var battle = states.Load("slot1").Battle;

        Assert.NotNull(battle);
        Assert.Equal("Grosh", battle!.FirstName);
        Assert.Equal("Elara", battle.SecondName);
        Assert.Equal(1, battle.Round);
        var entry = Assert.Single(battle.History);
        Assert.Equal("attack", entry.Action);
    }

    [Fact]
    public void Continue_WithoutAName_GoesOnFromTheLastSave_RightWhereTheBattleStopped()
    {
        var (states, results) = FirstRunSavedMidBattle();
        var second = new Session(states, results, "surrender"); // Elara is the one to move, so she gives up

        second.Cli.ExecCommand("continue");

        Assert.True(second.Displayer.Any("Continuing from the last save: 'slot1'"));
        Assert.True(second.Displayer.Any("Resuming the battle at round 1"));
        var result = Assert.Single(results.Items);
        Assert.Equal("Grosh", result.Winner);
        Assert.Equal("Elara", result.Loser);

        // The attack that was played before the save is still in the history that is shown at the end.
        Assert.True(second.Displayer.Any("attack"));
    }

    [Fact]
    public void Continue_WithAName_ResumesThatSave()
    {
        var (states, results) = FirstRunSavedMidBattle("evening");
        var second = new Session(states, results, "surrender");

        second.Cli.ExecCommand("continue evening");

        Assert.False(second.Displayer.Any("Continuing from the last save"));
        Assert.Equal("Grosh", Assert.Single(results.Items).Winner);
    }

    [Fact]
    public void Continue_RestoresTheHealthOfTheFighters()
    {
        var (states, results) = FirstRunSavedMidBattle();
        var saved = states.Load("slot1").Characters.Single(c => c.Name == "Elara");
        var second = new Session(states, results, "status", "surrender");

        second.Cli.ExecCommand("continue");

        Assert.True(saved.Health < 80); // the attack before the save really hurt her
        Assert.True(second.Displayer.Any($"{saved.Health}"));
    }

    [Fact]
    public void Continue_WithoutAnySaves_ReportsError()
    {
        var second = new Session(new InMemoryGameStateRepository(), new InMemoryGameResultRepository());

        second.Cli.ExecCommand("continue");

        Assert.True(second.Displayer.HasError("no saved games"));
    }

    [Fact]
    public void Continue_UnknownSave_ReportsError()
    {
        var second = new Session(new InMemoryGameStateRepository(), new InMemoryGameResultRepository());

        second.Cli.ExecCommand("continue missing");

        Assert.True(second.Displayer.HasError("Saved game 'missing' not found"));
    }

    [Fact]
    public void Continue_FromASaveWithoutABattle_OnlyLoadsTheWorld()
    {
        var states = new InMemoryGameStateRepository();
        var results = new InMemoryGameResultRepository();
        var first = new Session(states, results, "Grosh", "1");
        first.Cli.ExecCommand("create char");
        first.Cli.ExecCommand("save calm");

        var second = new Session(states, results);
        second.Cli.ExecCommand("continue");

        Assert.True(second.Displayer.HasSuccess("Game state 'calm' loaded"));
        Assert.True(second.Displayer.Any("no battle in progress"));
        Assert.Empty(results.Items);
    }

    [Fact]
    public void Load_OfASaveWithABattle_TellsHowToGoOn()
    {
        var (states, results) = FirstRunSavedMidBattle();
        var second = new Session(states, results);

        second.Cli.ExecCommand("load slot1");

        Assert.True(second.Displayer.HasSuccess("Game state 'slot1' loaded"));
        Assert.True(second.Displayer.Any("continue slot1"));
    }

    [Fact]
    public void SaveAfterAFinishedBattle_HasNoBattleToContinue()
    {
        var states = new InMemoryGameStateRepository();
        var results = new InMemoryGameResultRepository();
        var session = new Session(states, results, "Grosh", "1", "Elara", "2", "surrender");

        session.Cli.ExecCommand("create char");
        session.Cli.ExecCommand("create char");
        session.Cli.ExecCommand("play Grosh Elara");
        session.Cli.ExecCommand("save after");

        Assert.Null(states.Load("after").Battle);
    }

    [Fact]
    public void RemoteCharacterNarrative_IsKeptBySaveAndLoad()
    {
        var states = new InMemoryGameStateRepository();
        var api = new FakeGenshinApiClient();
        api.AddCharacter(new GameSystem.Core.Models.RemoteCharacterInfo(
            "diluc", "Diluc", "Darknight Hero", "Pyro", "Claymore", 5, "A wine merchant.", []));

        var first = CliFactory.Create(new FakeDisplayer(), new FakeInputReader(), new FakeCombatLogger(), states, new InMemoryGameResultRepository(), apiClient: api);
        first.ExecCommand("remote create diluc Duke");
        first.ExecCommand("save withremote");

        Assert.Equal("Diluc", states.Load("withremote").RemoteInfo!["Duke"].Name);

        // A new run of the program: the narrative data of the character is back, so its sheet is complete again.
        var displayer = new FakeDisplayer();
        var second = CliFactory.Create(displayer, new FakeInputReader(), new FakeCombatLogger(), states, new InMemoryGameResultRepository());
        second.ExecCommand("continue withremote");
        second.ExecCommand("sheet Duke");

        Assert.True(displayer.Any("Darknight Hero"));
    }
}
