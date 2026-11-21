using GameSystem.Core.Engine;

namespace GameSystem.Core.Models;

/// <summary>
/// Persisted state of a battle that is still going on. Fighters are referenced by name (their health, effects and
/// stance live in the character states). <paramref name="CurrentOrder"/> holds the order of actions of the current
/// round as indices into [first, second]: it cannot be recomputed after loading, because the effects have already
/// ticked at the start of that round.
/// </summary>
public sealed record BattleStateModel(
    string FirstName,
    string SecondName,
    bool SecondFighterIsAi,
    int Round,
    int TurnsPlayed,
    List<int> CurrentOrder,
    List<TurnEntry> History);
