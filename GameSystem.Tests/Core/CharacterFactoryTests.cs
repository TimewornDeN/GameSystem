using GameSystem.Core.Characters;
using GameSystem.Core.Management;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.Core;

public class CharacterFactoryTests
{
    [Theory]
    [InlineData("warrior", typeof(Warrior))]
    [InlineData("WARRIOR", typeof(Warrior))]
    [InlineData("  mage  ", typeof(Mage))]
    public void Create_ReturnsExpectedType_CaseAndWhitespaceInsensitive(string classId, Type expectedType)
    {
        var character = CharacterFactory.Create(classId, "Test", new FakeCombatLogger());

        Assert.IsType(expectedType, character);
    }

    [Fact]
    public void Create_UnknownClassId_Throws()
    {
        Assert.Throws<ArgumentException>(() => CharacterFactory.Create("necromancer", "Test", new FakeCombatLogger()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyClassId_Throws(string classId)
    {
        Assert.Throws<ArgumentException>(() => CharacterFactory.Create(classId, "Test", new FakeCombatLogger()));
    }
}
