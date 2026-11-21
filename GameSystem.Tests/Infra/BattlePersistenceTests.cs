using GameSystem.Core.Combat;
using GameSystem.Core.Engine;
using GameSystem.Core.Models;
using GameSystem.Infra.Persistence;
using Xunit;

namespace GameSystem.Tests.Infra;

/// <summary>Practical 15 on disk: the battle in progress, effects, remote data, "last save", safe writing, locations.</summary>
public class BattlePersistenceTests : IDisposable
{
    private readonly string _directory;
    private readonly JsonGameStateRepository _repository;

    public BattlePersistenceTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "GameSystemTests_" + Guid.NewGuid().ToString("N"));
        _repository = new JsonGameStateRepository(_directory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);

        GC.SuppressFinalize(this);
    }

    private static GameStateModel StateWithBattle() => new(
        Items: [],
        Abilities: [],
        Characters:
        [
            new CharacterStateModel("warrior", "Grosh", 120, null, [], IsDefending: true,
                Effects: [new StatusEffect("Armor Break", StatKind.Armor, -3, 2)]),
            new CharacterStateModel("mage", "Elara", 50, null, [])
        ],
        Battle: new BattleStateModel(
            "Grosh", "Elara", SecondFighterIsAi: true, Round: 2, TurnsPlayed: 3, CurrentOrder: [0, 1],
            History:
            [
                new TurnEntry(1, 1, "Grosh", "Elara", "attack", 150, 150, 80, 62),
                new TurnEntry(1, 2, "Elara", "Grosh", "defend", 62, 62, 150, 150),
                new TurnEntry(2, 3, "Grosh", "Elara", "attack", 150, 150, 62, 50)
            ]),
        RemoteInfo: new Dictionary<string, RemoteCharacterInfo>
        {
            ["Grosh"] = new("diluc", "Diluc", "Darknight Hero", "Pyro", "Claymore", 5, "A wine merchant.",
                [new RemoteAbilityInfo("Searing Onslaught", "A fiery combo.")])
        });

    private static void SetWriteTime(string directory, string name, DateTime utc)
        => File.SetLastWriteTimeUtc(Path.Combine(directory, name + ".json"), utc);

    [Fact]
    public void Battle_RoundTripsThroughTheFile()
    {
        var original = StateWithBattle();
        _repository.Save("slot", original);

        var loaded = _repository.Load("slot");

        var battle = Assert.IsType<BattleStateModel>(loaded.Battle);
        Assert.Equal("Grosh", battle.FirstName);
        Assert.Equal("Elara", battle.SecondName);
        Assert.True(battle.SecondFighterIsAi);
        Assert.Equal(2, battle.Round);
        Assert.Equal(3, battle.TurnsPlayed);
        Assert.Equal(original.Battle!.CurrentOrder, battle.CurrentOrder);
        Assert.Equal(original.Battle.History, battle.History);
    }

    [Fact]
    public void StanceAndEffects_RoundTripThroughTheFile()
    {
        _repository.Save("slot", StateWithBattle());

        var loaded = _repository.Load("slot");

        Assert.True(loaded.Characters[0].IsDefending);
        Assert.Equal([new StatusEffect("Armor Break", StatKind.Armor, -3, 2)], loaded.Characters[0].Effects);
        Assert.False(loaded.Characters[1].IsDefending);
        Assert.Null(loaded.Characters[1].Effects);
    }

    [Fact]
    public void RemoteInfo_RoundTripsThroughTheFile()
    {
        _repository.Save("slot", StateWithBattle());

        var loaded = _repository.Load("slot");

        var info = loaded.RemoteInfo!["Grosh"];
        Assert.Equal("Diluc", info.Name);
        Assert.Equal(5, info.Rarity);
        Assert.Equal("Claymore", info.WeaponType);
        Assert.Equal([new RemoteAbilityInfo("Searing Onslaught", "A fiery combo.")], info.Abilities);
    }

    [Fact]
    public void FileWrittenBeforeThisFeature_StillLoads()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            Path.Combine(_directory, "old.json"),
            """{"Items":[],"Abilities":[],"Characters":[{"ClassId":"mage","Name":"Elara","Health":80,"AbilityIds":[]}]}""");

        var loaded = _repository.Load("old");

        Assert.Single(loaded.Characters);
        Assert.Null(loaded.Battle);
        Assert.Null(loaded.RemoteInfo);
        Assert.False(loaded.Characters[0].IsDefending);
        Assert.Null(loaded.Characters[0].Effects);
    }

    [Fact]
    public void FileFromANewerVersion_IsRejectedWithAClearMessage()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            Path.Combine(_directory, "future.json"),
            """{"Version":99,"Items":[],"Abilities":[],"Characters":[]}""");

        var ex = Assert.Throws<InvalidDataException>(() => _repository.Load("future"));

        Assert.Contains("newer version", ex.Message);
    }

    [Fact]
    public void UnknownStatInAnEffect_IsReportedAsACorruptedSave()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            Path.Combine(_directory, "bad.json"),
            """{"Items":[],"Abilities":[],"Characters":[{"ClassId":"mage","Name":"Elara","Health":80,"AbilityIds":[],"Effects":[{"Name":"Luck","Stat":"Luck","Amount":5,"Turns":2}]}]}""");

        Assert.Throws<InvalidDataException>(() => _repository.Load("bad"));
    }

    [Fact]
    public void Latest_IsNullWithoutSaves()
    {
        Assert.Null(_repository.Latest());
    }

    [Fact]
    public void Latest_IsTheMostRecentlyWrittenSave()
    {
        var start = new DateTime(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc);

        _repository.Save("alpha", StateWithBattle());
        SetWriteTime(_directory, "alpha", start);
        _repository.Save("zeta", StateWithBattle());
        SetWriteTime(_directory, "zeta", start.AddMinutes(1));
        Assert.Equal("zeta", _repository.Latest());

        // Saving over an old slot makes it the latest again.
        _repository.Save("alpha", StateWithBattle());
        SetWriteTime(_directory, "alpha", start.AddMinutes(2));
        Assert.Equal("alpha", _repository.Latest());
    }

    [Fact]
    public void Save_LeavesNoTemporaryFileBehind_AndReplacesTheOldContent()
    {
        _repository.Save("slot", StateWithBattle());
        _repository.Save("slot", new GameStateModel([], [], []));

        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
        Assert.Null(_repository.Load("slot").Battle);
        Assert.Equal(["slot"], _repository.List());
    }

    [Fact]
    public void ResultsRepository_AlsoWritesWithoutLeavingTemporaryFiles()
    {
        string path = Path.Combine(_directory, "results", "game-results.json");
        var results = new JsonGameResultRepository(path);

        results.Add(new GameResult("A", "B", 3, 40, 100, new DateTime(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc)));
        results.Add(new GameResult("B", "A", 5, 10, 80, new DateTime(2026, 11, 15, 13, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(2, results.GetAll().Count);
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp"));
    }

    [Fact]
    public void AppDataLocations_UsesTheOverrideWhenThereIsOne()
    {
        Assert.Equal("custom-home", AppDataLocations.Resolve("custom-home", "/home/user/.local/share"));
    }

    [Fact]
    public void AppDataLocations_DefaultsToTheApplicationDataFolder()
    {
        string expected = Path.Combine("/home/user/.local/share", "GameSystem");

        Assert.Equal(expected, AppDataLocations.Resolve(null, "/home/user/.local/share"));
        Assert.Equal(expected, AppDataLocations.Resolve("   ", "/home/user/.local/share"));
    }

    [Fact]
    public void AppDataLocations_WithoutAnyFolder_StaysNextToTheProgram()
    {
        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "data"), AppDataLocations.Resolve(null, ""));
    }

    [Fact]
    public void AppDataLocations_PutsSavesAndResultsInSeparateSubfolders()
    {
        Assert.Equal(Path.Combine("root", "saves"), AppDataLocations.Saves("root"));
        Assert.Equal(Path.Combine("root", "results", "game-results.json"), AppDataLocations.Results("root"));
    }
}
