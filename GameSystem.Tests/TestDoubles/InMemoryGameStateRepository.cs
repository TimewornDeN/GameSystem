using GameSystem.Core.Abstractions;
using GameSystem.Core.Models;

namespace GameSystem.Tests.TestDoubles;

/// <summary>In-memory stand-in for the JSON repository, so CLI tests don't touch the file system.</summary>
public sealed class InMemoryGameStateRepository : IGameStateRepository
{
    private readonly Dictionary<string, GameStateModel> _store = new(StringComparer.OrdinalIgnoreCase);
    private string? _latest;

    public void Save(string name, GameStateModel state)
    {
        _store[name] = state;
        _latest = name;
    }

    public GameStateModel Load(string name)
        => _store.TryGetValue(name, out var state) ? state : throw new FileNotFoundException($"Saved game '{name}' not found.");

    public bool Exists(string name) => _store.ContainsKey(name);

    public IReadOnlyList<string> List() => [.. _store.Keys.Order()];

    public string? Latest() => _latest;
}
