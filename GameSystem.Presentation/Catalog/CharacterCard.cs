using GameSystem.Core.Characters;
using GameSystem.Core.Management;
using GameSystem.Core.Models;

namespace GameSystem.Presentation.Catalog;

/// <summary>
/// Everything one card of the catalog shows. It is a plain immutable value taken from the game world, so the
/// window never touches live game objects and two cards with the same data are equal (no needless redraw).
/// </summary>
public sealed record CharacterCard(
    string Name,
    string ClassId,
    int Health,
    int MaxHealth,
    int Attack,
    int Armor,
    int Resist,
    int Speed,
    string? RemoteId,
    string Title,
    string Subtitle,
    string Description,
    int Rarity,
    string ItemName,
    int AbilityCount,
    bool IsFighting)
{
    private const int MaxDescriptionLength = 140;

    /// <summary>First letter of the name, drawn on the placeholder when there is no portrait.</summary>
    public string Initial => Name.Length > 0 ? char.ToUpperInvariant(Name[0]).ToString() : "?";

    public string Stars => new('\u2605', Math.Clamp(Rarity, 0, 5));

    public double HealthPercent => MaxHealth <= 0 ? 0 : Math.Clamp(100.0 * Health / MaxHealth, 0, 100);

    public string HealthText => $"{Health}/{MaxHealth} HP";

    public string StatsText => $"ATK {Attack}  ARM {Armor}  RES {Resist}%  SPD {Speed}";

    public string ClassText => ClassId.Length == 0 ? string.Empty : char.ToUpperInvariant(ClassId[0]) + ClassId[1..];

    public static CharacterCard From(Character character, RemoteCharacterInfo? remote, bool isFighting)
    {
        ArgumentNullException.ThrowIfNull(character);

        string subtitle = remote is null
            ? string.Empty
            : string.Join(" \u00B7 ", new[] { remote.Element, remote.WeaponType }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new CharacterCard(
            character.Name,
            character.ClassId,
            character.Health,
            character.MaxHealth,
            character.EffectiveAttack,
            character.EffectiveArmor,
            character.EffectiveResist,
            character.EffectiveSpeed,
            remote?.Id,
            remote?.Title ?? string.Empty,
            subtitle,
            Shorten(remote?.Description ?? string.Empty),
            remote?.Rarity ?? 0,
            character.EquippedItem?.Name ?? string.Empty,
            character.Abilities.Count,
            isFighting);
    }

    /// <summary>Cards of every character in the roster, in roster order.</summary>
    public static IReadOnlyList<CharacterCard> BuildAll(GameWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var battle = world.Battle is { Session.IsFinished: false } b ? b.Session : null;

        return
        [
            .. world.Characters.Select(c => From(
                c,
                world.RemoteInfo.TryGetValue(c.Name, out var info) ? info : null,
                battle is not null && (ReferenceEquals(c, battle.First) || ReferenceEquals(c, battle.Second))))
        ];
    }

    private static string Shorten(string text)
    {
        string oneLine = text.ReplaceLineEndings(" ").Trim();
        return oneLine.Length <= MaxDescriptionLength ? oneLine : oneLine[..(MaxDescriptionLength - 1)].TrimEnd() + "\u2026";
    }
}
