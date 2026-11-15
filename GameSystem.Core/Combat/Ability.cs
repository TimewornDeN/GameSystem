using GameSystem.Core.Characters;
using GameSystem.Core.Combat.Strategies;
using GameSystem.Core.Models;

namespace GameSystem.Core.Combat;

/// <summary>
/// A usable ability. The damage algorithm is an injected <see cref="IDamageStrategy"/>:
/// custom abilities (CLI / save files) use a <see cref="FixedDamageStrategy"/>, built-in class abilities
/// may use any other strategy. Only fixed-power abilities can be converted back to a persistable model.
/// </summary>
public sealed class Ability
{
    private readonly IDamageStrategy _damageStrategy;

    public string Id { get; }
    public string Description { get; }
    public bool IgnoresArmor => _damageStrategy.IgnoresArmor;

    /// <summary>Percentage points of the target's resist that this ability ignores.</summary>
    public int ResistPenetration { get; }

    /// <summary>Buff or debuff applied after the hit (built-in abilities only; it is not persisted).</summary>
    public StatusEffect? Effect { get; }

    /// <summary>True when <see cref="Effect"/> goes to the caster (a buff), false when it goes to the target (a debuff).</summary>
    public bool EffectOnSelf { get; }

    /// <summary>Builds a custom ability with a fixed power value (e.g. loaded from a save file or the CLI).</summary>
    public Ability(AbilityModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (string.IsNullOrWhiteSpace(model.Id))
            throw new ArgumentException("Ability id cannot be empty.", nameof(model));

        Id = model.Id;
        Description = string.IsNullOrWhiteSpace(model.Description) ? model.Id : model.Description;
        _damageStrategy = new FixedDamageStrategy(model.Power, model.IgnoresArmor);
    }

    /// <summary>Builds an ability with an arbitrary damage strategy (used for built-in class abilities).</summary>
    public Ability(
        string id,
        string description,
        IDamageStrategy damageStrategy,
        StatusEffect? effect = null,
        bool effectOnSelf = false,
        int resistPenetration = 0)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ability id cannot be empty.", nameof(id));

        ArgumentNullException.ThrowIfNull(damageStrategy);

        Id = id;
        Description = string.IsNullOrWhiteSpace(description) ? id : description;
        _damageStrategy = damageStrategy;
        Effect = effect;
        EffectOnSelf = effectOnSelf;
        ResistPenetration = Math.Max(0, resistPenetration);
    }

    /// <summary>Computes the raw damage this ability deals before the target's armor mitigation is applied.</summary>
    public int ComputeDamage(Character actor, Character target)
        => _damageStrategy.Compute(actor, target);

    /// <summary>Builds the blow that goes into the damage formula: raw damage plus the armor and resist rules of this ability.</summary>
    public Hit CreateHit(Character actor, Character target)
        => new(_damageStrategy.ComputeRaw(actor, target), IgnoresArmor, ResistPenetration);

    /// <summary>Converts this ability to a persistable model. Power is only meaningful for fixed-power abilities.</summary>
    public AbilityModel ToModel()
        => new(Id, Description, _damageStrategy is FixedDamageStrategy fixedStrategy ? fixedStrategy.Power : 0, IgnoresArmor);
}
