using GameSystem.Core.Abstractions;
using GameSystem.Core.Models;

namespace GameSystem.Tests.TestDoubles;

/// <summary>In-memory stand-in for the JSON results file. It can be told to fail, to test error handling.</summary>
public sealed class InMemoryGameResultRepository : IGameResultRepository
{
    public List<GameResult> Items { get; } = [];

    public bool FailOnAdd { get; set; }

    public void Add(GameResult result)
    {
        if (FailOnAdd)
            throw new IOException("disk full");

        Items.Add(result);
    }

    public IReadOnlyList<GameResult> GetAll() => [.. Items];
}
