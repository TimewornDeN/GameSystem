using GameSystem.Core.Characters;

namespace GameSystem.Core.Combat.Strategies;

/// <summary>
/// STRATEGY (damage calculation). Each ability delegates "how much raw damage do I deal" to an
/// interchangeable algorithm instead of storing a nullable fixed value next to a nullable delegate.
/// New damage rules (percentage of target HP, scaling with armor...) are new classes, not edits to <see cref="Ability"/>.
/// </summary>
public interface IDamageStrategy
{
    /// <summary>True when the damage is pre-compensated so the target's armor does not reduce it.</summary>
    bool IgnoresArmor { get; }

    /// <summary>Computes raw damage before the target's own armor mitigation is applied.</summary>
    int Compute(Character actor, Character target);

    /// <summary>
    /// The damage before ANY defence, without the armor pre-compensation of <see cref="Compute"/>.
    /// The damage formula (<see cref="DamageCalculator"/>) uses this value together with the <see cref="IgnoresArmor"/> flag.
    /// </summary>
    int ComputeRaw(Character actor, Character target) => Compute(actor, target);
}
