using GameSystem.Core.Characters;
using GameSystem.Core.Combat.Actions;

namespace GameSystem.Core.Engine;

/// <summary>
/// STRATEGY (turn decision). Decides which <see cref="ICombatAction"/> a character performs on its turn.
/// The battle engine does not care whether the answer is random, scripted or comes from an AI.
/// </summary>
public interface ITurnStrategy
{
    ICombatAction ChooseAction(Character actor, Character opponent);
}
