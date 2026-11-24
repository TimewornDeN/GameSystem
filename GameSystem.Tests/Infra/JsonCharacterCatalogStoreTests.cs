using GameSystem.Core.Models;
using GameSystem.Infra.Persistence;
using Xunit;

namespace GameSystem.Tests.Infra;

public class JsonCharacterCatalogStoreTests
{
    private static string NewTempFile() => Path.Combine(Path.GetTempPath(), "gamesystem-catalog-" + Guid.NewGuid().ToString("N"), "catalog", "characters.json");

    private static void Cleanup(string file)
    {
        string? root = Path.GetDirectoryName(Path.GetDirectoryName(file));
        if (root is not null && Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Load_ReturnsAnEmptyList_WhenNothingWasSavedYet()
    {
        string file = NewTempFile();

        Assert.Empty(new JsonCharacterCatalogStore(file).Load());
    }

    [Fact]
    public void SavedCharacters_ComeBackWithAllTheirFields()
    {
        string file = NewTempFile();
        try
        {
            var store = new JsonCharacterCatalogStore(file);
            var albedo = new RemoteCharacterInfo("albedo", "Albedo", "Kreideprinz", "Geo", "Sword", 5, "Chief alchemist.",
                [new RemoteAbilityInfo("Favonius Bladework", "Normal attack")]);

            store.Save([albedo]);

            var loaded = Assert.Single(new JsonCharacterCatalogStore(file).Load());
            Assert.Equal(albedo.Id, loaded.Id);
            Assert.Equal("Kreideprinz", loaded.Title);
            Assert.Equal("Geo", loaded.Element);
            Assert.Equal(5, loaded.Rarity);
            Assert.Equal("Favonius Bladework", Assert.Single(loaded.Abilities).Name);
            Assert.False(File.Exists(file + ".tmp"));
        }
        finally
        {
            Cleanup(file);
        }
    }

    [Fact]
    public void Load_OfACorruptedFile_ExplainsWhatIsWrong()
    {
        string file = NewTempFile();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, "{ this is not json");

            var error = Assert.Throws<InvalidDataException>(() => new JsonCharacterCatalogStore(file).Load());
            Assert.Contains("corrupted", error.Message);
        }
        finally
        {
            Cleanup(file);
        }
    }

    [Fact]
    public void TheCatalogFile_LiesInItsOwnFolderInsideTheDataFolder()
    {
        string path = AppDataLocations.Catalog(Path.Combine("data", "root"));

        Assert.Equal(Path.Combine("data", "root", "catalog", "characters.json"), path);
    }
}
