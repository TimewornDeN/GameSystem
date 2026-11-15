using GameSystem.CLI;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Models;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.CLI;

/// <summary>Practical 14 through the command line: loading new characters, the history of a battle, saving during a battle.</summary>
public class BusinessLogicCliTests
{
    private sealed class StubProfileSource : ICharacterProfileSource
    {
        private readonly Dictionary<string, CharacterProfile> _profiles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["zed"] = new CharacterProfile("mage", "Zed", new ItemModel("Cape", ResistBonus: 20), [new AbilityModel("bolt", "Bolt", 30)]),
            ["twin"] = new CharacterProfile("warrior", "Zed"),
            ["bad"] = new CharacterProfile("necromancer", "Bob")
        };

        public IReadOnlyList<string> List() => [.. _profiles.Keys.Order()];

        public CharacterProfile Load(string name)
            => _profiles.TryGetValue(name, out var profile) ? profile : throw new FileNotFoundException($"Character profile '{name}' not found.");
    }

    private sealed class Setup
    {
        public Setup(IEnumerable<string?> inputs, bool withProfiles = true)
        {
            Cli = CliFactory.Create(
                Displayer, new FakeInputReader([.. inputs]), new FakeCombatLogger(), States, new InMemoryGameResultRepository(),
                profiles: withProfiles ? new StubProfileSource() : null);
        }

        public FakeDisplayer Displayer { get; } = new();

        public InMemoryGameStateRepository States { get; } = new();

        public Cli Cli { get; }
    }

    [Fact]
    public void Import_AddsTheCharacterWithItsItemAndAbilities()
    {
        var setup = new Setup([]);

        setup.Cli.ExecCommand("import zed");

        Assert.True(setup.Displayer.HasSuccess("Zed"));

        setup.Cli.ExecCommand("ls char");
        Assert.True(setup.Displayer.Any("Zed"));

        setup.Cli.ExecCommand("ls item");
        Assert.True(setup.Displayer.Any("Cape"));
    }

    [Fact]
    public void Import_SameCharacterTwice_ReportsError()
    {
        var setup = new Setup([]);

        setup.Cli.ExecCommand("import zed");
        setup.Cli.ExecCommand("import twin"); // another profile with the same character name

        Assert.True(setup.Displayer.HasError("already exists"));
    }

    [Fact]
    public void Import_UnknownOrBrokenProfile_ReportsError()
    {
        var setup = new Setup([]);

        setup.Cli.ExecCommand("import nobody");
        setup.Cli.ExecCommand("import bad");

        Assert.Equal(2, setup.Displayer.Errors.Count(e => e.Contains("Could not load")));
    }

    [Fact]
    public void Import_WithoutName_ShowsUsage_AndListShowsProfiles()
    {
        var setup = new Setup([]);

        setup.Cli.ExecCommand("import");
        setup.Cli.ExecCommand("import --list");

        Assert.True(setup.Displayer.HasError("Usage: import"));
        Assert.True(setup.Displayer.Any("Available profiles: bad, twin, zed"));
    }

    [Fact]
    public void Import_IsNotAvailable_WhenNoProfileSourceIsPlugged()
    {
        var setup = new Setup([], withProfiles: false);

        setup.Cli.ExecCommand("import zed");

        Assert.True(setup.Displayer.Any("Unknown command"));
    }

    [Fact]
    public void Battle_HistoryCommand_ShowsTheActionsSoFar()
    {
        var setup = new Setup(["Grosh", "1", "Elara", "2", "history", "attack", "history", "surrender"]);
        setup.Cli.ExecCommand("create char");
        setup.Cli.ExecCommand("create char");

        setup.Cli.ExecCommand("play Grosh Elara");

        Assert.True(setup.Displayer.Any("Nothing has happened yet"));
        Assert.True(setup.Displayer.Any("Elara -18 HP (80 -> 62)"));
    }

    [Fact]
    public void Battle_End_ShowsTheWholeHistory()
    {
        var setup = new Setup(["Grosh", "1", "Elara", "2", "attack", "surrender"]);
        setup.Cli.ExecCommand("create char");
        setup.Cli.ExecCommand("create char");

        setup.Cli.ExecCommand("play Grosh Elara");

        Assert.True(setup.Displayer.Any("Battle history"));
        Assert.True(setup.Displayer.Any("attack"));
    }

    [Fact]
    public void Battle_SaveCommand_SavesTheGameInTheMiddleOfAFight()
    {
        var setup = new Setup(["Grosh", "1", "Elara", "2", "attack", "save slot1", "surrender"]);
        setup.Cli.ExecCommand("create char");
        setup.Cli.ExecCommand("create char");

        setup.Cli.ExecCommand("play Grosh Elara");

        Assert.True(setup.States.Exists("slot1"));
        var elara = setup.States.Load("slot1").Characters.Single(c => c.Name == "Elara");
        Assert.Equal(62, elara.Health);
    }
}
