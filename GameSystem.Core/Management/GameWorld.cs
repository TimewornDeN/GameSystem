using GameSystem.Core.Characters;
using GameSystem.Core.Combat;
using GameSystem.Core.Engine;
using GameSystem.Core.Models;

namespace GameSystem.Core.Management;

/// <summary>
/// The current game world shared by all commands: catalogs of items and abilities, the roster of characters
/// and the narrative data of characters that were created from the remote database.
/// </summary>
public sealed class GameWorld
{
    public List<Character> Characters { get; } = [];

    public List<Item> Items { get; } = [];

    public List<Ability> Abilities { get; } = [];

    /// <summary>Narrative data fetched from the remote API, by character name.</summary>
    public Dictionary<string, RemoteCharacterInfo> RemoteInfo { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The battle that is going on or waits to be continued after a load; null when there is none.
    /// Saving a game includes it, and the command line clears it when the battle ends.
    /// </summary>
    public ResumableBattle? Battle { get; set; }

    public Character? FindCharacter(string idOrName)
        => Characters.FirstOrDefault(c => c.Name.Equals(idOrName, StringComparison.OrdinalIgnoreCase));

    public Item? FindItem(string name)
        => Items.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public Ability? FindAbility(string id)
        => Abilities.FirstOrDefault(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Replaces the whole roster and catalogs with a restored (loaded) world.</summary>
    public void ReplaceWith(RestoredWorld restored)
    {
        ArgumentNullException.ThrowIfNull(restored);

        Items.Clear();
        Items.AddRange(restored.Items);

        Abilities.Clear();
        Abilities.AddRange(restored.Abilities);

        Characters.Clear();
        Characters.AddRange(restored.Characters);

        RemoteInfo.Clear();
        if (restored.RemoteInfo is not null)
        {
            foreach (var (name, info) in restored.RemoteInfo)
                RemoteInfo[name] = info;
        }

        Battle = restored.Battle;
    }
}
