using GameSystem.Core.Models;

namespace GameSystem.Presentation.Catalog;

/// <summary>
/// Everything one card of the server catalog shows: a character that the remote database offers, whether or not
/// it was added to the game. An immutable value taken from the downloaded data.
/// </summary>
public sealed record ServerCharacterCard(
    string Id,
    string Name,
    string Title,
    string Subtitle,
    string Description,
    int Rarity,
    int TalentCount)
{
    /// <summary>First letter of the name, drawn on the placeholder when there is no portrait.</summary>
    public string Initial => Name.Length > 0 ? char.ToUpperInvariant(Name[0]).ToString() : "?";

    public string Stars => new('\u2605', Math.Clamp(Rarity, 0, 5));

    public string TalentsText => $"Talents: {TalentCount}";

    public static ServerCharacterCard From(RemoteCharacterInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        string subtitle = string.Join(" \u00B7 ", new[] { info.Element, info.WeaponType }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new ServerCharacterCard(
            info.Id,
            info.Name,
            info.Title,
            subtitle,
            CharacterCard.Shorten(info.Description ?? string.Empty),
            info.Rarity,
            info.Abilities?.Count ?? 0);
    }
}
