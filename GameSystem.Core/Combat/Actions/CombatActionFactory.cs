using GameSystem.Core.Characters;

namespace GameSystem.Core.Combat.Actions;

/// <summary>Translates an action name typed by the user ("attack", "heal"...) into the matching command.</summary>
public static class CombatActionFactory
{
    /// <summary>
    /// Returns the command for <paramref name="actionName"/>, or null when the name is unknown
    /// or the action needs a target that was not supplied ("defend" is the only target-less action).
    /// </summary>
    public static ICombatAction? Create(string actionName, Character actor, Character? target, string? abilityId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
        ArgumentNullException.ThrowIfNull(actor);

        return actionName.Trim().ToLowerInvariant() switch
        {
            "defend" => new DefendAction(actor),
            "attack" when target is not null => new AttackAction(actor, target),
            "heal" when target is not null => new HealAction(target),
            "ability" when target is not null => new UseAbilityAction(actor, target, abilityId),
            _ => null
        };
    }
}
