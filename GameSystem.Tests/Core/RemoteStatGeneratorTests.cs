using GameSystem.Core.Management;
using GameSystem.Core.Models;
using Xunit;

namespace GameSystem.Tests.Core;

public class RemoteStatGeneratorTests
{
    [Fact]
    public void GenerateRarityBonusItem_FiveStar_RollsInUpperRange()
    {
        var item = RemoteStatGenerator.GenerateRarityBonusItem(5, new Random(Seed: 1));

        Assert.Equal("5\u2605 Potential", item.Name);
        Assert.InRange(item.HealthBonus, 30, 60);
        Assert.InRange(item.ArmorBonus, 5, 10);
        Assert.InRange(item.AttackBonus, 15, 30);
    }

    [Fact]
    public void GenerateRarityBonusItem_FourStar_RollsInLowerRange()
    {
        var item = RemoteStatGenerator.GenerateRarityBonusItem(4, new Random(Seed: 1));

        Assert.Equal("4\u2605 Potential", item.Name);
        Assert.InRange(item.HealthBonus, 10, 30);
        Assert.InRange(item.AttackBonus, 5, 15);
    }

    [Fact]
    public void GenerateRarityBonusItem_IsDeterministic_ForTheSameSeed()
    {
        var first = RemoteStatGenerator.GenerateRarityBonusItem(5, new Random(Seed: 42));
        var second = RemoteStatGenerator.GenerateRarityBonusItem(5, new Random(Seed: 42));

        Assert.Equal(first.Model, second.Model);
    }

    [Fact]
    public void GenerateAbilities_RespectsMaxCount()
    {
        List<RemoteAbilityInfo> remote =
        [
            new("Normal Attack", "Performs up to 5 consecutive strikes."),
            new("Elemental Skill", "Unleashes elemental power."),
            new("Elemental Burst", "Unleashes a powerful elemental attack.")
        ];

        var abilities = RemoteStatGenerator.GenerateAbilities(remote, rarity: 5, maxCount: 2);

        Assert.Equal(2, abilities.Count);
    }

    [Fact]
    public void GenerateAbilities_SlugifiesNameIntoId()
    {
        List<RemoteAbilityInfo> remote = [new("Elemental Skill: Blazing Arc", "...")];

        var abilities = RemoteStatGenerator.GenerateAbilities(remote, rarity: 5);

        Assert.Equal("elemental-skill-blazing-arc", abilities[0].Id);
        Assert.Equal("Elemental Skill: Blazing Arc", abilities[0].Description);
    }

    [Fact]
    public void GenerateAbilities_PowerScalesWithRarity_AndIncreasesPerAbility()
    {
        List<RemoteAbilityInfo> remote = [new("Skill A", "..."), new("Skill B", "...")];

        var abilities = RemoteStatGenerator.GenerateAbilities(remote, rarity: 4);

        Assert.Equal(40, abilities[0].ToModel().Power);
        Assert.Equal(45, abilities[1].ToModel().Power);
    }
}
