using GameSystem.Core.Characters;

namespace GameSystem.Core.Combat.Actions;

/// <summary>Puts a character into a defensive stance (double armor for the next hit).</summary>
public sealed class DefendAction(Character actor) : ICombatAction
{
    private readonly Character _actor = actor ?? throw new ArgumentNullException(nameof(actor));

    public void Execute() => _actor.Defend();
}
