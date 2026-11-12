using System.Text.Json;
using System.Text.Json.Serialization;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Models;
using GameSystem.Infra.Dto;
using GameSystem.Infra.Mapping;

namespace GameSystem.Infra.Persistence;

/// <summary>Stores every saved game as a separate JSON file inside a directory.</summary>
public class JsonGameStateRepository : IGameStateRepository
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _directory;

    public JsonGameStateRepository(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Directory path cannot be empty.", nameof(directory));

        _directory = directory;
    }

    public void Save(string name, GameStateModel state)
    {
        ArgumentNullException.ThrowIfNull(state);

        string path = GetPath(name);
        Directory.CreateDirectory(_directory);

        string json = JsonSerializer.Serialize(GameStateMapper.ToDto(state), Options);
        File.WriteAllText(path, json);
    }

    public GameStateModel Load(string name)
    {
        string path = GetPath(name);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Saved game '{name}' not found.", path);

        try
        {
            var dto = JsonSerializer.Deserialize<GameStateDto>(File.ReadAllText(path), Options)
                      ?? throw new InvalidDataException("File is empty.");
            return GameStateMapper.ToModel(dto);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            throw new InvalidDataException($"Saved game '{name}' is corrupted: {ex.Message}", ex);
        }
    }

    public bool Exists(string name) => File.Exists(GetPath(name));

    public IReadOnlyList<string> List()
    {
        if (!Directory.Exists(_directory))
            return [];

        return [.. Directory.EnumerateFiles(_directory, "*.json")
            .Select(file => Path.GetFileNameWithoutExtension(file))
            .Order()];
    }

    private string GetPath(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Invalid save name.", nameof(name));

        return Path.Combine(_directory, name + ".json");
    }
}
