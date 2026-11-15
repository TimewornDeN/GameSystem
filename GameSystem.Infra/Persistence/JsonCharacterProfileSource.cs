using System.Text.Json;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Models;

namespace GameSystem.Infra.Persistence;

/// <summary>
/// Reads new characters from a folder: every <c>name.json</c> file is one <see cref="CharacterProfile"/>.
/// The file layout is the shape of the Core model itself, names are matched without regard to case.
/// </summary>
public class JsonCharacterProfileSource : ICharacterProfileSource
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string _directory;

    public JsonCharacterProfileSource(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Directory path cannot be empty.", nameof(directory));

        _directory = directory;
    }

    public IReadOnlyList<string> List()
    {
        if (!Directory.Exists(_directory))
            return [];

        return [.. Directory.EnumerateFiles(_directory, "*.json")
            .Select(file => Path.GetFileNameWithoutExtension(file))
            .Order()];
    }

    public CharacterProfile Load(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Invalid profile name.", nameof(name));

        string path = Path.Combine(_directory, name + ".json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Character profile '{name}' not found.", path);

        try
        {
            var profile = JsonSerializer.Deserialize<CharacterProfile>(File.ReadAllText(path), Options)
                          ?? throw new InvalidDataException("File is empty.");

            if (string.IsNullOrWhiteSpace(profile.ClassId) || string.IsNullOrWhiteSpace(profile.Name))
                throw new InvalidDataException("A profile needs 'classId' and 'name'.");

            return profile;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Character profile '{name}' is corrupted: {ex.Message}", ex);
        }
    }
}
