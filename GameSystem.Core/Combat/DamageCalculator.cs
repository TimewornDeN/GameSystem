namespace GameSystem.Core.Combat;

/// <summary>
/// The damage formula of the game in one pure place (no state, no logging), so it can be tested with plain numbers
/// and described in the design document as is.
/// <code>
/// armor_eff  = ignoresArmor ? 0 : armor
/// afterArmor = max(MinDamage, raw - armor_eff)
/// resist_eff = clamp(resist - resistPenetration, 0, MaxResist)
/// damage     = max(MinDamage, floor(afterArmor * (100 - resist_eff) / 100))
/// </code>
/// </summary>
public static class DamageCalculator
{
    /// <summary>A hit that lands always scratches the target for at least this much, so a fight cannot stall forever.</summary>
    public const int MinDamage = 1;

    /// <summary>Resist is capped so that nobody becomes immune.</summary>
    public const int MaxResist = 75;

    /// <summary>Computes the health lost by a target with the given (already effective) armor and resist.</summary>
    public static int Resolve(Hit hit, int armor, int resist)
    {
        if (hit.Amount < 0)
            throw new ArgumentException("Damage cannot be negative.", nameof(hit));

        int armorUsed = hit.IgnoresArmor ? 0 : Math.Max(0, armor);
        int afterArmor = Math.Max(MinDamage, hit.Amount - armorUsed);

        int resistUsed = Math.Clamp(resist - Math.Max(0, hit.ResistPenetration), 0, MaxResist);

        return Math.Max(MinDamage, afterArmor * (100 - resistUsed) / 100);
    }
}
