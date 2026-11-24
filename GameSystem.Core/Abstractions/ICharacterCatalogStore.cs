using GameSystem.Core.Models;

namespace GameSystem.Core.Abstractions;

/// <summary>
/// Local copy of the list of characters of the remote database, so the catalog opens at once and also works
/// without a network. Core only knows the contract; Infra keeps the copy in a JSON file.
/// </summary>
public interface ICharacterCatalogStore
{
    /// <summary>The saved catalog; an empty list when nothing was saved yet.</summary>
    IReadOnlyList<RemoteCharacterInfo> Load();

    /// <summary>Replaces the saved catalog.</summary>
    void Save(IReadOnlyList<RemoteCharacterInfo> characters);
}
