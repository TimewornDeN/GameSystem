using GameSystem.Core.Characters;

namespace GameSystem.Core.Combat.Actions;

/// <summary>Restores health to a character (the target may be the actor itself).</summary>
public sealed class HealAction(Character target, int amount = HealAction.DefaultAmount) : ICombatAction
{
    /// <summary>Amount restored by the standard heal action.</summary>
    public const int DefaultAmount = 20;

    private readonly Character _target = target ?? throw new ArgumentNullException(nameof(target));
    private readonly int _amount = amount;

    public void Execute() => _target.Heal(_amount);
}
