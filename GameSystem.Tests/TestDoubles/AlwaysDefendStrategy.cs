using GameSystem.Core.Characters;
using GameSystem.Core.Combat.Actions;
using GameSystem.Core.Engine;

namespace GameSystem.Tests.TestDoubles;

/// <summary>A scripted opponent that only ever defends, so battles against it are fully predictable.</summary>
public sealed class AlwaysDefendStrategy : ITurnStrategy
{
    public ICombatAction ChooseAction(Character actor, Character opponent) => new DefendAction(actor);
}
