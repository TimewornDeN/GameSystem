using System.Text.Json;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Models;

namespace GameSystem.Infra.Persistence;

/// <summary>Keeps the history of finished games in one JSON file.</summary>
public class JsonGameResultRepository : IGameResultRepository
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string _filePath;

    public JsonGameResultRepository(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));

        _filePath = filePath;
    }

    public void Add(GameResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var all = Read();
        all.Add(result);

        AtomicFile.WriteAllText(_filePath, JsonSerializer.Serialize(all, Options));
    }

    public IReadOnlyList<GameResult> GetAll() => Read();

    private List<GameResult> Read()
    {
        if (!File.Exists(_filePath))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<GameResult>>(File.ReadAllText(_filePath), Options) ?? [];
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"The results file is corrupted: {ex.Message}", ex);
        }
    }
}
