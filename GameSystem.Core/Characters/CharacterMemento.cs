using GameSystem.Core.Combat;

namespace GameSystem.Core.Characters;

/// <summary>
/// Immutable snapshot of a character's mutable state (the Memento of the MEMENTO pattern).
/// Only <see cref="Character"/> creates and applies it; callers (the caretaker) just store it.
/// <see cref="Item"/> and <see cref="Ability"/> are immutable, so sharing them with the live character is safe.
/// </summary>
/// <param name="ClassId">Class of the character the snapshot belongs to.</param>
/// <param name="Name">Name of the character the snapshot belongs to.</param>
/// <param name="Health">Current health at the moment of the snapshot.</param>
/// <param name="EquippedItem">Equipped item, if any.</param>
/// <param name="LearnedAbilities">Abilities learned on top of the innate one.</param>
/// <param name="IsDefending">Whether the defensive stance was active.</param>
public sealed record CharacterMemento(
    string ClassId,
    string Name,
    int Health,
    Item? EquippedItem,
    IReadOnlyList<Ability> LearnedAbilities,
    bool IsDefending);
