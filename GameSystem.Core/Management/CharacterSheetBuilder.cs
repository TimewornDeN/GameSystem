using GameSystem.Core.Characters;
using GameSystem.Core.Models;
using GameSystem.Core.Text;

namespace GameSystem.Core.Management;

/// <summary>
/// Builds a structured-text document (the Text module) describing a character's current state,
/// optionally enriched with narrative data fetched from the remote character database.
/// </summary>
public static class CharacterSheetBuilder
{
    public static Root Build(Character character, RemoteCharacterInfo? remoteInfo = null)
    {
        ArgumentNullException.ThrowIfNull(character);

        var root = new Root($"Character Sheet: {character.Name}");

        var heading = new Heading(remoteInfo is null ? character.Name : $"{character.Name} \u2014 {remoteInfo.Title}");
        root.AddChild(heading);

        string classLine = remoteInfo is null
            ? $"Class: {character.ClassId}"
            : $"Class: {character.ClassId} | Element: {remoteInfo.Element} | Weapon type: {remoteInfo.WeaponType} | Rarity: {remoteInfo.Rarity}\u2605";
        heading.AddChild(new Paragraph(classLine));

        heading.AddChild(new Paragraph($"HP: {character.Health}/{character.MaxHealth} | Armor: {character.Armor} | Attack: {character.AttackPower} | Resist: {character.Resist}% | Speed: {character.Speed}"));

        heading.AddChild(new Paragraph(
            string.IsNullOrWhiteSpace(remoteInfo?.Description)
                ? "(no remote description available)"
                : remoteInfo.Description));

        heading.AddChild(new Paragraph(
            character.EquippedItem is null ? "Equipped: (nothing)" : $"Equipped: {character.EquippedItem.Name}"));

        var abilitiesHeading = new Heading("Abilities", heading.Rank + 1);
        heading.AddChild(abilitiesHeading);
        foreach (var ability in character.Abilities)
        {
            abilitiesHeading.AddChild(new Paragraph($"{ability.Id}: {ability.Description}"));
        }

        if (remoteInfo is not null)
        {
            heading.AddChild(new Link("Portrait", $"https://genshin.jmp.blue/characters/{remoteInfo.Id}/portrait"));
        }

        return root;
    }
}
