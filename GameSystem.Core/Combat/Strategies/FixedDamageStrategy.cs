using GameSystem.Core.Characters;

namespace GameSystem.Core.Combat.Strategies;

/// <summary>Deals a constant amount of damage; used by custom abilities and by armor-piercing spells.</summary>
public sealed class FixedDamageStrategy(int power, bool ignoresArmor = false) : IDamageStrategy
{
    public int Power { get; } = power;

    public bool IgnoresArmor { get; } = ignoresArmor;

    /// <inheritdoc />
    /// <remarks>
    /// Armor piercing is modelled by adding the target's armor up-front, so that
    /// <see cref="Character.TakeDamage"/> cancels it out when it subtracts the armor again.
    /// </remarks>
    public int Compute(Character actor, Character target)
        => IgnoresArmor ? Power + target.Armor : Power;

    /// <inheritdoc />
    public int ComputeRaw(Character actor, Character target) => Power;
}
