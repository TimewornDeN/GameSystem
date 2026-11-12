using GameSystem.Core.Characters;

namespace GameSystem.Core.Combat.Strategies;

/// <summary>Deals damage proportional to the caster's current attack power (so equipment bonuses count).</summary>
public sealed class AttackScalingDamageStrategy(int multiplier) : IDamageStrategy
{
    public int Multiplier { get; } = multiplier;

    public bool IgnoresArmor => false;

    public int Compute(Character actor, Character target)
        => actor.AttackPower * Multiplier;
}
