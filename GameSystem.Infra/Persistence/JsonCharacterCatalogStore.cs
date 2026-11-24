using System.Text.Json;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Models;

namespace GameSystem.Infra.Persistence;

/// <summary>Keeps the downloaded catalog of characters in one JSON file, so it is available without the network.</summary>
public sealed class JsonCharacterCatalogStore : ICharacterCatalogStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string _filePath;

    public JsonCharacterCatalogStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));

        _filePath = filePath;
    }

    public IReadOnlyList<RemoteCharacterInfo> Load()
    {
        if (!File.Exists(_filePath))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<RemoteCharacterInfo>>(File.ReadAllText(_filePath), Options) ?? [];
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"The catalog file is corrupted: {ex.Message}", ex);
        }
    }

    public void Save(IReadOnlyList<RemoteCharacterInfo> characters)
    {
        ArgumentNullException.ThrowIfNull(characters);

        AtomicFile.WriteAllText(_filePath, JsonSerializer.Serialize(characters, Options));
    }
}
