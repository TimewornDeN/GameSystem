using GameSystem.Core.Combat;
using GameSystem.Core.Models;

namespace GameSystem.Core.Management;

/// <summary>
/// The remote character database carries no combat statistics (only narrative data), so the
/// assignment allows generating starter stats/abilities from the character's rarity instead.
/// Kept separate from HTTP concerns and fully deterministic given an injected Random, so it's testable.
/// </summary>
public static class RemoteStatGenerator
{
    /// <summary>
    /// Builds a single equippable "starter gear" item whose bonuses scale with rarity:
    /// 4-star characters roll in the lower half of the range, 5-star in the upper half.
    /// </summary>
    public static Item GenerateRarityBonusItem(int rarity, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        (int min, int max) = rarity >= 5 ? (15, 30) : (5, 15);

        int healthBonus = random.Next(min, max + 1) * 2;
        int armorBonus = random.Next(min, max + 1) / 3;
        int attackBonus = random.Next(min, max + 1);

        return new Item(new ItemModel($"{rarity}\u2605 Potential", healthBonus, armorBonus, attackBonus));
    }

    /// <summary>
    /// Converts up to <paramref name="maxCount"/> remote talents into learnable abilities with a
    /// fixed, deterministic power derived from rarity (no randomness, so results stay reproducible).
    /// </summary>
    public static List<Ability> GenerateAbilities(IReadOnlyList<RemoteAbilityInfo> remoteAbilities, int rarity, int maxCount = 2)
    {
        ArgumentNullException.ThrowIfNull(remoteAbilities);

        var abilities = new List<Ability>();
        int power = rarity * 10;

        foreach (var remote in remoteAbilities.Take(Math.Max(0, maxCount)))
        {
            string id = Slugify(remote.Name);
            if (string.IsNullOrEmpty(id)) continue;

            abilities.Add(new Ability(new AbilityModel(id, remote.Name, power)));
            power += 5; // Each subsequent ability is slightly stronger, for a bit of variety.
        }

        return abilities;
    }

    private static string Slugify(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        var chars = name.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : ' ');

        var slug = string.Join('-', new string([.. chars])
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return slug.Length > 40 ? slug[..40] : slug;
    }
}
