using GameSystem.Core.Models;
using GameSystem.Infra.Persistence;
using Xunit;

namespace GameSystem.Tests.Infra;

/// <summary>Uses a fresh temp directory per test instance so tests never collide or leave files behind.</summary>
public class JsonGameStateRepositoryTests : IDisposable
{
    private readonly string _directory;
    private readonly JsonGameStateRepository _repository;

    public JsonGameStateRepositoryTests()
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

    private static GameStateModel SampleState() => new(
        Items: [new ItemModel("Flaming Sword", HealthBonus: 20, ArmorBonus: 0, AttackBonus: 15)],
        Abilities: [new AbilityModel("shield-bash", "Shield Bash", 25, IgnoresArmor: false)],
        Characters: [new CharacterStateModel("warrior", "Grosh", 120, "Flaming Sword", ["shield-bash"])]
    );

    [Fact]
    public void Exists_ReturnsFalse_ForUnknownSave()
    {
        Assert.False(_repository.Exists("missing"));
    }

    [Fact]
    public void Save_ThenExists_ReturnsTrue()
    {
        _repository.Save("battle-1", SampleState());

        Assert.True(_repository.Exists("battle-1"));
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsAllData()
    {
        var original = SampleState();
        _repository.Save("battle-1", original);

        var loaded = _repository.Load("battle-1");

        Assert.Single(loaded.Items);
        Assert.Equal(original.Items[0], loaded.Items[0]);

        Assert.Single(loaded.Abilities);
        Assert.Equal(original.Abilities[0], loaded.Abilities[0]);

        Assert.Single(loaded.Characters);
        var expectedChar = original.Characters[0];
        var actualChar = loaded.Characters[0];
        Assert.Equal(expectedChar.ClassId, actualChar.ClassId);
        Assert.Equal(expectedChar.Name, actualChar.Name);
        Assert.Equal(expectedChar.Health, actualChar.Health);
        Assert.Equal(expectedChar.EquippedItemName, actualChar.EquippedItemName);
        Assert.Equal(expectedChar.AbilityIds, actualChar.AbilityIds);
    }

    [Fact]
    public void Load_UnknownSave_ThrowsFileNotFound()
    {
        Assert.Throws<FileNotFoundException>(() => _repository.Load("missing"));
    }

    [Fact]
    public void Load_CorruptedFile_ThrowsInvalidData()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "corrupt.json"), "{ not valid json ");

        Assert.Throws<InvalidDataException>(() => _repository.Load("corrupt"));
    }

    [Fact]
    public void List_ReturnsSavedNames_InAlphabeticalOrder()
    {
        _repository.Save("zeta", SampleState());
        _repository.Save("alpha", SampleState());

        var names = _repository.List();

        Assert.Equal(["alpha", "zeta"], names);
    }

    [Fact]
    public void List_WhenDirectoryDoesNotExistYet_ReturnsEmpty()
    {
        Assert.Empty(_repository.List());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Save_RejectsInvalidNames(string name)
    {
        Assert.Throws<ArgumentException>(() => _repository.Save(name, SampleState()));
    }
}
