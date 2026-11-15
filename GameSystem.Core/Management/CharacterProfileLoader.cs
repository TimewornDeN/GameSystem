using GameSystem.Core.Characters;
using GameSystem.Core.Combat;
using GameSystem.Core.Logging;
using GameSystem.Core.Models;

namespace GameSystem.Core.Management;

/// <summary>A character built from a profile together with the item and abilities that came with it.</summary>
public sealed record LoadedCharacter(Character Character, Item? Item, IReadOnlyList<Ability> Abilities);

/// <summary>
/// CharacterProfile -> Character. Pure business rule: how outside data becomes a live fighter.
/// The caller decides where to put the result (the game world), the loader never touches it.
/// </summary>
public static class CharacterProfileLoader
{
    public static LoadedCharacter Build(CharacterProfile profile, ICombatLogger logger)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(logger);

        var character = CharacterFactory.Create(profile.ClassId, profile.Name, logger);

        Item? item = profile.Item is null ? null : new Item(profile.Item);
        if (item is not null)
            character.Equip(item);

        List<Ability> abilities = [];
        foreach (var model in profile.Abilities ?? [])
        {
            var ability = new Ability(model);
            character.Learn(ability);
            abilities.Add(ability);
        }

        return new LoadedCharacter(character, item, abilities);
    }
}
