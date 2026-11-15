using GameSystem.Infra.WebApi;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.Infra;

public class HttpGenshinApiClientTests
{
    private static HttpGenshinApiClient CreateClient(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://genshin.jmp.blue/") };
        return new HttpGenshinApiClient(httpClient);
    }

    [Fact]
    public async Task GetCharacterIdsAsync_ReturnsParsedList()
    {
        var handler = new StubHttpMessageHandler()
            .RespondWith("characters", """["albedo","amber","klee"]""");
        var client = CreateClient(handler);

        var ids = await client.GetCharacterIdsAsync();

        Assert.Equal(["albedo", "amber", "klee"], ids);
    }

    [Fact]
    public async Task GetCharacterAsync_MapsFieldsToRemoteCharacterInfo()
    {
        const string json = """
        {
            "name": "Albedo",
            "title": "Kreideprinz",
            "vision": "Geo",
            "weapon": "Sword",
            "rarity": 5,
            "description": "The Chief Alchemist of the Knights of Favonius.",
            "skillTalents": [
                { "name": "Favonius Bladework", "description": "Performs up to 5 strikes." },
                { "name": "Abiogenesis: Solar Isotoma", "description": "Creates a Fatal Blossom field." }
            ]
        }
        """;
        var handler = new StubHttpMessageHandler().RespondWith("characters/albedo?lang=en", json);
        var client = CreateClient(handler);

        var info = await client.GetCharacterAsync("albedo");

        Assert.Equal("albedo", info.Id);
        Assert.Equal("Albedo", info.Name);
        Assert.Equal("Kreideprinz", info.Title);
        Assert.Equal("Geo", info.Element);
        Assert.Equal("Sword", info.WeaponType);
        Assert.Equal(5, info.Rarity);
        Assert.Equal(2, info.Abilities.Count);
        Assert.Equal("Favonius Bladework", info.Abilities[0].Name);
    }

    [Fact]
    public async Task GetCharacterAsync_MissingOptionalFields_DefaultToEmptyStrings()
    {
        const string json = """{ "name": "Mystery", "rarity": 4 }""";
        var handler = new StubHttpMessageHandler().RespondWith("characters/mystery?lang=en", json);
        var client = CreateClient(handler);

        var info = await client.GetCharacterAsync("mystery");

        Assert.Equal(string.Empty, info.Title);
        Assert.Equal(string.Empty, info.Element);
        Assert.Equal(string.Empty, info.WeaponType);
        Assert.Empty(info.Abilities);
    }

    [Fact]
    public async Task GetWeaponIdsAsync_ReturnsParsedList()
    {
        var handler = new StubHttpMessageHandler()
            .RespondWith("weapons", """["skyward-blade","aquila-favonia"]""");
        var client = CreateClient(handler);

        var ids = await client.GetWeaponIdsAsync();

        Assert.Equal(["skyward-blade", "aquila-favonia"], ids);
    }

    [Fact]
    public async Task GetWeaponAsync_MapsFieldsToRemoteWeaponInfo()
    {
        const string json = """
        {
            "id": "skyward-blade",
            "name": "Skyward Blade",
            "type": "Sword",
            "rarity": 5,
            "baseAttack": 44,
            "passiveName": "Sky-Piercing Fang",
            "passiveDesc": "CRIT Rate increased by 4%.",
            "description": "The sword of a knight."
        }
        """;
        var handler = new StubHttpMessageHandler().RespondWith("weapons/skyward-blade?lang=en", json);
        var client = CreateClient(handler);

        var weapon = await client.GetWeaponAsync("skyward-blade");

        Assert.Equal("skyward-blade", weapon.Id);
        Assert.Equal("Skyward Blade", weapon.Name);
        Assert.Equal("Sword", weapon.Type);
        Assert.Equal(5, weapon.Rarity);
        Assert.Equal(44, weapon.BaseAttack);
        Assert.Equal("Sky-Piercing Fang", weapon.PassiveName);
    }

    [Fact]
    public async Task GetCharacterAsync_UnknownId_ThrowsInvalidOperation()
    {
        var handler = new StubHttpMessageHandler(); // nothing registered -> 404
        var client = CreateClient(handler);

        await Assert.ThrowsAnyAsync<Exception>(() => client.GetCharacterAsync("does-not-exist"));
    }
}
