using GameSystem.Core.Models;

namespace GameSystem.Core.Abstractions;

/// <summary>Persistence of finished games (the history of results).</summary>
public interface IGameResultRepository
{
    void Add(GameResult result);

    IReadOnlyList<GameResult> GetAll();
}
