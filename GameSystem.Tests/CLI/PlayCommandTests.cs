using GameSystem.CLI;
using GameSystem.Core.Engine;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.CLI;

/// <summary>The whole game flow through the facade: choosing fighters, the main loop, saving the result.</summary>
public class PlayCommandTests
{
    private static readonly DateTime FixedNow = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    private sealed class Setup
    {
        public Setup(IEnumerable<string?> inputs, ITurnStrategy? ai = null)
        {
            Input = new FakeInputReader([.. inputs]);
            Cli = CliFactory.Create(
                Displayer, Input, new FakeCombatLogger(), States, Results,
                opponentStrategy: ai, clock: () => FixedNow);
        }

        public FakeDisplayer Displayer { get; } = new();

        public FakeInputReader Input { get; }

        public InMemoryGameStateRepository States { get; } = new();

        public InMemoryGameResultRepository Results { get; } = new();

        public Cli Cli { get; }
    }

    /// <summary>Answers of the dialogs that create Grosh (warrior) and Elara (mage), followed by the given extra input.</summary>
    private static Setup TwoFighters(params string?[] afterCreation)
    {
        var setup = new Setup(["Grosh", "1", "Elara", "2", .. afterCreation]);
        setup.Cli.ExecCommand("create char");
        setup.Cli.ExecCommand("create char");
        return setup;
    }

    private static string?[] FightScript(int rounds) =>
        [.. Enumerable.Range(0, rounds).SelectMany(_ => new[] { "attack", "defend" })];

    [Fact]
    public void Play_WithoutEnoughCharacters_ReportsError()
    {
        var setup = new Setup([]);

        setup.Cli.ExecCommand("play");

        Assert.True(setup.Displayer.HasError("At least two living characters"));
    }

    [Fact]
    public void Play_UnknownCharacter_ReportsError()
    {
        var setup = TwoFighters();

        setup.Cli.ExecCommand("play Grosh Nobody");

        Assert.True(setup.Displayer.HasError("Character 'Nobody' not found"));
        Assert.Empty(setup.Results.Items);
    }

    [Fact]
    public void Play_SameCharacterTwice_ReportsError()
    {
        var setup = TwoFighters();

        setup.Cli.ExecCommand("play Grosh Grosh");

        Assert.True(setup.Displayer.HasError("two different characters"));
    }

    [Fact]
    public void Play_FirstFighterSurrenders_SecondWinsAndResultIsSaved()
    {
        var setup = TwoFighters("surrender");

        setup.Cli.ExecCommand("play Grosh Elara");

        var result = Assert.Single(setup.Results.Items);
        Assert.Equal("Elara", result.Winner);
        Assert.Equal("Grosh", result.Loser);
        Assert.Equal(FixedNow, result.FinishedAtUtc);
        Assert.True(setup.Displayer.HasSuccess("WINNER: [Elara]"));
        Assert.True(setup.Displayer.HasSuccess("result was saved"));
    }

    [Fact]
    public void Play_WithoutNames_LetsTheUserChooseFromTheList_ByNumberAndName()
    {
        var setup = TwoFighters("9", "1", "1", "Elara", "surrender");

        setup.Cli.ExecCommand("play");

        Assert.True(setup.Displayer.Any("Choose your fighters"));
        Assert.Contains(setup.Displayer.Entries, e => e.Text.Contains("Invalid choice"));
        Assert.Contains(setup.Displayer.Entries, e => e.Text.Contains("already chosen"));
        Assert.Equal("Elara", Assert.Single(setup.Results.Items).Winner);
    }

    [Fact]
    public void Play_FightToTheEnd_WinnerIsSavedWithRounds()
    {
        // Grosh only attacks, Elara only defends, so Grosh must win well within 20 rounds.
        var setup = TwoFighters(FightScript(20));

        setup.Cli.ExecCommand("play Grosh Elara");

        var result = Assert.Single(setup.Results.Items);
        Assert.Equal("Grosh", result.Winner);
        Assert.Equal("Elara", result.Loser);
        Assert.InRange(result.Rounds, 1, 20);
        Assert.True(result.WinnerHealth > 0);
    }

    [Fact]
    public void Play_AgainstTheComputer_OnlyTheFirstFighterNeedsInput()
    {
        var setup = new Setup(["Grosh", "1", "Elara", "2", .. Enumerable.Repeat("attack", 20)], new AlwaysDefendStrategy());
        setup.Cli.ExecCommand("create char");
        setup.Cli.ExecCommand("create char");

        setup.Cli.ExecCommand("play Grosh Elara --ai");

        Assert.Equal("Grosh", Assert.Single(setup.Results.Items).Winner);
        Assert.True(setup.Displayer.Any("(AI) is making a move"));
    }

    [Fact]
    public void Play_InputEndsDuringTheBattle_AbandonsWithoutSavingAResult()
    {
        var setup = TwoFighters(); // no battle input left

        setup.Cli.ExecCommand("play Grosh Elara");

        Assert.Empty(setup.Results.Items);
        Assert.True(setup.Displayer.Any("battle was abandoned"));
    }

    [Fact]
    public void Battle_UnknownCommandKeepsTheTurn_AndStatusShowsTheFighters()
    {
        var setup = TwoFighters("dance", "status", "surrender");

        setup.Cli.ExecCommand("play Grosh Elara");

        Assert.Contains(setup.Displayer.Entries, e => e.Text.Contains("Unknown command 'dance'"));
        Assert.True(setup.Displayer.Any("Turn of Grosh"));
        Assert.Equal("Elara", Assert.Single(setup.Results.Items).Winner);
    }

    [Fact]
    public void Battle_Help_ListsTheBattleCommands()
    {
        var setup = TwoFighters("help", "surrender");

        setup.Cli.ExecCommand("play Grosh Elara");

        Assert.True(setup.Displayer.Any("[Battle]"));
        Assert.Contains(setup.Displayer.Entries, e => e.Text.TrimStart().StartsWith("attack "));
        Assert.Contains(setup.Displayer.Entries, e => e.Text.TrimStart().StartsWith("surrender "));
    }

    [Fact]
    public void Battle_FailedAbility_ReportsErrorAndKeepsTheTurn()
    {
        var setup = TwoFighters("ability does-not-exist", "surrender");

        setup.Cli.ExecCommand("play Grosh Elara");

        Assert.True(setup.Displayer.HasError("does not know ability"));
        // Grosh still had the turn, so it was Grosh who surrendered.
        Assert.Equal("Elara", Assert.Single(setup.Results.Items).Winner);
    }

    [Fact]
    public void Play_WhenTheResultCannotBeSaved_ReportsError()
    {
        var setup = TwoFighters("surrender");
        setup.Results.FailOnAdd = true;

        setup.Cli.ExecCommand("play Grosh Elara");

        Assert.True(setup.Displayer.HasError("Could not save the result"));
    }

    [Fact]
    public void Results_WithoutGames_ShowsInfo()
    {
        var setup = new Setup([]);

        setup.Cli.ExecCommand("results");

        Assert.True(setup.Displayer.Any("No finished games yet"));
    }

    [Fact]
    public void Results_AfterAGame_ShowsATable()
    {
        var setup = TwoFighters("surrender");
        setup.Cli.ExecCommand("play Grosh Elara");

        setup.Cli.ExecCommand("results");

        Assert.True(setup.Displayer.Any("Winner HP"));
        Assert.True(setup.Displayer.Any("Elara"));
    }

    [Fact]
    public void FightDamage_IsKeptInTheWorld_AndCanBeSaved()
    {
        var setup = TwoFighters(FightScript(20));
        setup.Cli.ExecCommand("play Grosh Elara");

        setup.Cli.ExecCommand("save after-battle");
        setup.Cli.ExecCommand("ls char --id Elara");

        Assert.True(setup.States.Exists("after-battle"));
        Assert.True(setup.Displayer.Any("HP: 0/"));
    }
}
