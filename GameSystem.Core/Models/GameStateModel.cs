namespace GameSystem.Core.Models;

/// <summary>Persisted state of a single character: class, vitals, equipment and known abilities.</summary>
public record CharacterStateModel(
    string ClassId,
    string Name,
    int Health,
    string? EquippedItemName,
    List<string> AbilityIds);

/// <summary>Full persisted snapshot of the game world: catalogs plus the roster of characters.</summary>
public record GameStateModel(
    List<ItemModel> Items,
    List<AbilityModel> Abilities,
    List<CharacterStateModel> Characters);
