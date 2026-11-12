using GameSystem.Core.Characters;
using GameSystem.Core.Combat.Actions;
using GameSystem.Core.Models;

namespace GameSystem.Core.Engine;

/// <summary>
/// A battle that is played step by step: somebody (the user, a script or an AI strategy) hands in one
/// action per turn. <see cref="Game"/> runs a whole battle automatically; this class is the interactive version
/// of the same rules (two fighters, they alternate, the round counter grows after the second fighter acted).
/// </summary>
public sealed class BattleSession
{
    private readonly Character[] _fighters;
    private int _turn;
    private Character? _surrendered;

    public BattleSession(Character first, Character second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        if (ReferenceEquals(first, second))
            throw new ArgumentException("A character cannot fight itself.", nameof(second));

        if (first.Health <= 0 || second.Health <= 0)
            throw new ArgumentException("Both fighters must be alive.");

        _fighters = [first, second];
    }

    public Character First => _fighters[0];

    public Character Second => _fighters[1];

    public Character CurrentActor => _fighters[_turn];

    public Character CurrentOpponent => _fighters[1 - _turn];

    /// <summary>Starts at 1 and grows after both fighters have acted.</summary>
    public int Round { get; private set; } = 1;

    /// <summary>How many turns have been played so far.</summary>
    public int TurnsPlayed { get; private set; }

    public bool IsFinished => _surrendered is not null || First.Health <= 0 || Second.Health <= 0;

    public Character? Winner
    {
        get
        {
            if (!IsFinished) return null;
            if (_surrendered is not null) return ReferenceEquals(_surrendered, First) ? Second : First;
            return First.Health > 0 ? First : Second;
        }
    }

    public Character? Loser => Winner is null ? null : ReferenceEquals(Winner, First) ? Second : First;

    /// <summary>
    /// Translates an action name ("attack", "heal", "defend", "ability") into a command for the current actor.
    /// Heal targets the actor itself, attack and ability target the opponent. Returns null for unknown names.
    /// </summary>
    public ICombatAction? CreateAction(string actionName, string? abilityId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);

        bool healsSelf = actionName.Trim().Equals("heal", StringComparison.OrdinalIgnoreCase);
        Character target = healsSelf ? CurrentActor : CurrentOpponent;

        return CombatActionFactory.Create(actionName, CurrentActor, target, abilityId);
    }

    /// <summary>
    /// Executes the action for the current actor and passes the turn.
    /// If the action throws, nothing changes and the same actor keeps the turn.
    /// </summary>
    public void Perform(ICombatAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (IsFinished)
            throw new InvalidOperationException("The battle is already finished.");

        action.Execute();
        TurnsPlayed++;

        if (IsFinished) return;

        if (_turn == 1) Round++;
        _turn = 1 - _turn;
    }

    /// <summary>The current actor gives up; the opponent wins.</summary>
    public void Surrender()
    {
        if (IsFinished)
            throw new InvalidOperationException("The battle is already finished.");

        _surrendered = CurrentActor;
    }

    public GameResult ToResult(DateTime finishedAtUtc)
    {
        if (Winner is null || Loser is null)
            throw new InvalidOperationException("The battle is not finished yet.");

        return new GameResult(Winner.Name, Loser.Name, Round, Winner.Health, Winner.MaxHealth, finishedAtUtc);
    }
}
