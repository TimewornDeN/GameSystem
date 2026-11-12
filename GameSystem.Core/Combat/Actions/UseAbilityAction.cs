using GameSystem.Core.Characters;

namespace GameSystem.Core.Combat.Actions;

/// <summary>Uses an ability on a target: the innate one when no id is given, otherwise the named known ability.</summary>
public sealed class UseAbilityAction(Character actor, Character target, string? abilityId = null) : ICombatAction
{
    private readonly Character _actor = actor ?? throw new ArgumentNullException(nameof(actor));
    private readonly Character _target = target ?? throw new ArgumentNullException(nameof(target));
    private readonly string? _abilityId = abilityId;

    public void Execute()
    {
        if (string.IsNullOrWhiteSpace(_abilityId))
            _actor.UseAbility(_target);
        else
            _actor.UseAbility(_abilityId, _target);
    }
}
