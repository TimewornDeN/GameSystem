namespace GameSystem.Core.Models;

/// <summary>
/// A ready-to-play description of a new character that comes from outside the game (a JSON file, a server):
/// its class, name, an optional starting item and any number of extra abilities.
/// </summary>
public sealed record CharacterProfile(
    string ClassId,
    string Name,
    ItemModel? Item = null,
    List<AbilityModel>? Abilities = null);
