using GameSystem.Core.Models;
using GameSystem.Infra.Mapping;
using GameSystem.Infra.Persistence;
using Xunit;

namespace GameSystem.Tests.Infra;

public sealed class JsonCharacterProfileSourceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "gamesystem-profiles-" + Guid.NewGuid().ToString("N"));

    public JsonCharacterProfileSourceTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private void Write(string name, string json) => File.WriteAllText(Path.Combine(_directory, name + ".json"), json);

    [Fact]
    public void Load_ReadsAProfileWithItemAndAbilities()
    {
        Write("zed", """
            {
              "classId": "mage",
              "name": "Zed",
              "item": { "name": "Cape", "resistBonus": 20, "speedBonus": 10 },
              "abilities": [ { "id": "bolt", "description": "Bolt", "power": 30, "ignoresArmor": true } ]
            }
            """);

        var profile = new JsonCharacterProfileSource(_directory).Load("zed");

        Assert.Equal("mage", profile.ClassId);
        Assert.Equal("Zed", profile.Name);
        Assert.Equal(20, profile.Item!.ResistBonus);
        Assert.Equal(10, profile.Item.SpeedBonus);
        var ability = Assert.Single(profile.Abilities!);
        Assert.Equal(30, ability.Power);
        Assert.True(ability.IgnoresArmor);
    }

    [Fact]
    public void Load_MinimalProfile_HasNoItemAndNoAbilities()
    {
        Write("bob", """{ "classId": "warrior", "name": "Bob" }""");

        var profile = new JsonCharacterProfileSource(_directory).Load("bob");

        Assert.Null(profile.Item);
        Assert.Null(profile.Abilities);
    }

    [Fact]
    public void List_ReturnsProfileNamesInOrder_AndIsEmptyWithoutTheFolder()
    {
        Write("b", """{ "classId": "mage", "name": "B" }""");
        Write("a", """{ "classId": "mage", "name": "A" }""");

        Assert.Equal(["a", "b"], new JsonCharacterProfileSource(_directory).List());
        Assert.Empty(new JsonCharacterProfileSource(Path.Combine(_directory, "missing")).List());
    }

    [Fact]
    public void Load_MissingFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => new JsonCharacterProfileSource(_directory).Load("nobody"));
    }

    [Fact]
    public void Load_BrokenJsonOrMissingFields_ThrowsInvalidData()
    {
        Write("broken", "{ not json");
        Write("empty", """{ "classId": "", "name": "" }""");
        var source = new JsonCharacterProfileSource(_directory);

        Assert.Throws<InvalidDataException>(() => source.Load("broken"));
        Assert.Throws<InvalidDataException>(() => source.Load("empty"));
    }

    [Fact]
    public void Load_NameWithPathCharacters_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new JsonCharacterProfileSource(_directory).Load("../secret"));
    }

    [Fact]
    public void StateMapper_RoundTripsResistAndSpeedBonuses()
    {
        var state = new GameStateModel(
            Items: [new ItemModel("Cape", ArmorBonus: 1, ResistBonus: 20, SpeedBonus: 10)],
            Abilities: [],
            Characters: []);

        var back = GameStateMapper.ToModel(GameStateMapper.ToDto(state));

        Assert.Equal(20, back.Items[0].ResistBonus);
        Assert.Equal(10, back.Items[0].SpeedBonus);
    }
}
