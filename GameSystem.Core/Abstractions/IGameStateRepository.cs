using GameSystem.Core.Models;

namespace GameSystem.Core.Abstractions;

/// <summary>Persistence of the whole game world (catalogs + characters) under a named save slot.</summary>
public interface IGameStateRepository
{
    void Save(string name, GameStateModel state);
    GameStateModel Load(string name);
    bool Exists(string name);
    IReadOnlyList<string> List();

    /// <summary>Name of the most recently written save, or null when there are no saves at all.</summary>
    string? Latest();
}
