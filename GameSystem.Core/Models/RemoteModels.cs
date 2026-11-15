namespace GameSystem.Core.Models;

/// <summary>One ability/talent as described by the remote character database.</summary>
public record RemoteAbilityInfo(string Name, string Description);

/// <summary>
/// Pure snapshot of a character fetched from the remote API (see IGenshinApiClient),
/// adapted to the fields our application actually needs.
/// </summary>
public record RemoteCharacterInfo(
    string Id,
    string Name,
    string Title,
    string Element,
    string WeaponType,
    int Rarity,
    string Description,
    List<RemoteAbilityInfo> Abilities);

/// <summary>Pure snapshot of a weapon fetched from the remote API.</summary>
public record RemoteWeaponInfo(
    string Id,
    string Name,
    string Type,
    int Rarity,
    int BaseAttack,
    string? PassiveName,
    string? PassiveDescription,
    string Description);
