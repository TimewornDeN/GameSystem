namespace GameSystem.Core.Combat.Actions;

/// <summary>
/// COMMAND. A combat move (attack, heal, defend, use ability) packaged as an object with all of its
/// arguments. The CLI builds these from text input and the battle engine receives them from a turn
/// strategy, so "deciding what to do" and "doing it" are decoupled and both sides share one code path.
/// </summary>
public interface ICombatAction
{
    void Execute();
}
