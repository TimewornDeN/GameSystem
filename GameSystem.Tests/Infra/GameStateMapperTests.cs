using GameSystem.Core.Models;
using GameSystem.Infra.Mapping;
using Xunit;

namespace GameSystem.Tests.Infra;

public class GameStateMapperTests
{
    private static GameStateModel SampleState() => new(
        Items: [new ItemModel("Flaming Sword", HealthBonus: 20, ArmorBonus: 0, AttackBonus: 15)],
        Abilities: [new AbilityModel("shield-bash", "Shield Bash", 25, IgnoresArmor: false)],
        Characters: [new CharacterStateModel("warrior", "Grosh", 120, "Flaming Sword", ["shield-bash"])]
    );

    [Fact]
    public void ToDto_ThenToModel_RoundTripsItems()
    {
        var original = SampleState();

        var roundTripped = GameStateMapper.ToModel(GameStateMapper.ToDto(original));

        Assert.Equal(original.Items[0], roundTripped.Items[0]);
    }

    [Fact]
    public void ToDto_ThenToModel_RoundTripsAbilities()
    {
        var original = SampleState();

        var roundTripped = GameStateMapper.ToModel(GameStateMapper.ToDto(original));

        Assert.Equal(original.Abilities[0], roundTripped.Abilities[0]);
    }

    [Fact]
    public void ToDto_ThenToModel_RoundTripsCharacters()
    {
        var original = SampleState();

        var roundTripped = GameStateMapper.ToModel(GameStateMapper.ToDto(original));

        var expected = original.Characters[0];
        var actual = roundTripped.Characters[0];

        Assert.Equal(expected.ClassId, actual.ClassId);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Health, actual.Health);
        Assert.Equal(expected.EquippedItemName, actual.EquippedItemName);
        Assert.Equal(expected.AbilityIds, actual.AbilityIds);
    }

    [Fact]
    public void ToDto_PreservesNullEquippedItemName()
    {
        var state = new GameStateModel(
            Items: [],
            Abilities: [],
            Characters: [new CharacterStateModel("mage", "Elara", 80, null, [])]
        );

        var dto = GameStateMapper.ToDto(state);

        Assert.Null(dto.Characters[0].EquippedItemName);
    }
}
