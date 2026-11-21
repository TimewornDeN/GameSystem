using GameSystem.Core.Combat;

namespace GameSystem.Core.Models;

/// <summary>Persisted state of a single character: class, vitals, equipment, known abilities, stance and active effects.</summary>
public record CharacterStateModel(
    string ClassId,
    string Name,
    int Health,
    string? EquippedItemName,
    List<string> AbilityIds,
    bool IsDefending = false,
    List<StatusEffect>? Effects = null);

/// <summary>
/// Full persisted snapshot of the game world: catalogs, the roster of characters, the battle that is in progress
/// (if any) and the narrative data of characters created from the remote database.
/// </summary>
public record GameStateModel(
    List<ItemModel> Items,
    List<AbilityModel> Abilities,
    List<CharacterStateModel> Characters,
    BattleStateModel? Battle = null,
    Dictionary<string, RemoteCharacterInfo>? RemoteInfo = null);
