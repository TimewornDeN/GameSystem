using GameSystem.Core.Abstractions;
using GameSystem.Core.Models;

namespace GameSystem.Tests.TestDoubles;

/// <summary>In-memory stand-in for the catalog file, so tests don't touch the file system.</summary>
public sealed class InMemoryCharacterCatalogStore : ICharacterCatalogStore
{
    public List<RemoteCharacterInfo> Items { get; } = [];

    public int SaveCount { get; private set; }

    public IReadOnlyList<RemoteCharacterInfo> Load() => [.. Items];

    public void Save(IReadOnlyList<RemoteCharacterInfo> characters)
    {
        Items.Clear();
        Items.AddRange(characters);
        SaveCount++;
    }
}
