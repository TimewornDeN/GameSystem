using GameSystem.Core.Characters;

namespace GameSystem.Core.Combat.Actions;

/// <summary>Basic attack of one character against another.</summary>
public sealed class AttackAction(Character actor, Character target) : ICombatAction
{
    private readonly Character _actor = actor ?? throw new ArgumentNullException(nameof(actor));
    private readonly Character _target = target ?? throw new ArgumentNullException(nameof(target));

    public void Execute() => _actor.Attack(_target);
}
