using GameSystem.Core.Models;

namespace GameSystem.Core.Rendering;

/// <summary>The history of finished games.</summary>
public sealed class GameResultsRenderable(IReadOnlyList<GameResult> results)
    : Renderable<IReadOnlyList<GameResult>>(results);
