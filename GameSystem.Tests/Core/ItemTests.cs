using GameSystem.Core.Combat;
using GameSystem.Core.Models;
using Xunit;

namespace GameSystem.Tests.Core;

public class ItemTests
{
    [Fact]
    public void Constructor_WithOnlyName_DefaultsBonusesToZero()
    {
        var item = new Item("Plain Ring");

        Assert.Equal("Plain Ring", item.Name);
        Assert.Equal(0, item.HealthBonus);
        Assert.Equal(0, item.ArmorBonus);
        Assert.Equal(0, item.AttackBonus);
    }

    [Fact]
    public void Constructor_SetsAllBonuses()
    {
        var item = new Item("Flaming Sword", healthBonus: 20, armorBonus: 2, attackBonus: 15);

        Assert.Equal(20, item.HealthBonus);
        Assert.Equal(2, item.ArmorBonus);
        Assert.Equal(15, item.AttackBonus);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyName(string name)
    {
        Assert.Throws<ArgumentException>(() => new Item(name));
    }

    [Fact]
    public void Constructor_RejectsNegativeBonuses()
    {
        Assert.Throws<ArgumentException>(() => new Item(new ItemModel("Cursed Ring", HealthBonus: -1)));
        Assert.Throws<ArgumentException>(() => new Item(new ItemModel("Cursed Ring", ArmorBonus: -1)));
        Assert.Throws<ArgumentException>(() => new Item(new ItemModel("Cursed Ring", AttackBonus: -1)));
    }

    [Fact]
    public void Model_RoundTripsThroughConstructor()
    {
        var model = new ItemModel("Shield", HealthBonus: 10, ArmorBonus: 5, AttackBonus: 0);
        var item = new Item(model);

        Assert.Equal(model, item.Model);
    }
}
