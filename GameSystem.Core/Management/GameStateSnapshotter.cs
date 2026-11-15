using GameSystem.Core.Characters;
using GameSystem.Core.Combat;
using GameSystem.Core.Logging;
using GameSystem.Core.Models;

namespace GameSystem.Core.Management;

/// <summary>The live objects rebuilt from a saved <see cref="GameStateModel"/>.</summary>
public sealed record RestoredWorld(List<Item> Items, List<Ability> Abilities, List<Character> Characters);

/// <summary>
/// MEMENTO (caretaker side, whole world). Turns the live game world into a persistable
/// <see cref="GameStateModel"/> and back, using <see cref="CharacterMemento"/> so that the CLI never has to
/// know how a character stores its equipment or abilities.
/// </summary>
public static class GameStateSnapshotter
{
    public static GameStateModel Capture(
        IEnumerable<Item> items,
        IEnumerable<Ability> abilities,
        IEnumerable<Character> characters)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(characters);

        return new GameStateModel(
            Items: [.. items.Select(i => i.Model)],
            Abilities: [.. abilities.Select(a => a.ToModel())],
            Characters: [.. characters.Select(ToStateModel)]);
    }

    /// <summary>
    /// Rebuilds the whole world. References to items or abilities that no longer exist in the catalogs
    /// are skipped, exactly like the previous CLI-side loading code did.
    /// </summary>
    public static RestoredWorld Restore(GameStateModel state, ICombatLogger logger)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(logger);

        List<Item> items = [.. state.Items.Select(m => new Item(m))];
        List<Ability> abilities = [.. state.Abilities.Select(m => new Ability(m))];
        List<Character> characters = [];

        foreach (var saved in state.Characters)
        {
            var character = CharacterFactory.Create(saved.ClassId, saved.Name, logger);

            Item? equipped = string.IsNullOrWhiteSpace(saved.EquippedItemName)
                ? null
                : items.FirstOrDefault(i => i.Name.Equals(saved.EquippedItemName, StringComparison.OrdinalIgnoreCase));

            List<Ability> learned = [];
            foreach (string abilityId in saved.AbilityIds)
            {
                var ability = abilities.FirstOrDefault(a => a.Id.Equals(abilityId, StringComparison.OrdinalIgnoreCase));
                if (ability is null) continue;

                bool alreadyKnown = ability.Id.Equals(character.DefaultAbilityId, StringComparison.OrdinalIgnoreCase)
                    || learned.Any(l => l.Id.Equals(ability.Id, StringComparison.OrdinalIgnoreCase));
                if (!alreadyKnown) learned.Add(ability);
            }

            character.Restore(new CharacterMemento(
                character.ClassId, character.Name, saved.Health, equipped, learned, IsDefending: false));

            characters.Add(character);
        }

        return new RestoredWorld(items, abilities, characters);
    }

    private static CharacterStateModel ToStateModel(Character character)
    {
        var memento = character.CreateMemento();

        return new CharacterStateModel(
            memento.ClassId,
            memento.Name,
            memento.Health,
            memento.EquippedItem?.Name,
            [.. memento.LearnedAbilities.Select(a => a.Id)]);
    }
}
