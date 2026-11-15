using GameSystem.Core.Characters;
using GameSystem.Core.Combat.Actions;
using GameSystem.Core.Models;

namespace GameSystem.Core.Engine;

/// <summary>
/// A battle that is played step by step: somebody (the user, a script or an AI strategy) hands in one
/// action per turn. <see cref="Game"/> runs a whole battle automatically; this class is the interactive version
/// of the same rules. It is the "Game" of the class diagram: it is started from a <see cref="GameConfig"/>,
/// plays a sequence of <see cref="Turn"/> objects and keeps their history.
/// </summary>
public sealed class BattleSession
{
    private readonly Character[] _fighters;
    private readonly List<Turn> _turns = [];
    private Turn _turn;
    private Character? _surrendered;

    public BattleSession(GameConfig config)
        : this(Require(config).Fighters[0], config.Fighters[1])
    {
    }

    public BattleSession(Character first, Character second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        if (ReferenceEquals(first, second))
            throw new ArgumentException("A character cannot fight itself.", nameof(second));

        if (first.Health <= 0 || second.Health <= 0)
            throw new ArgumentException("Both fighters must be alive.");

        _fighters = [first, second];

        // Buffs and debuffs belong to one battle only.
        first.ClearEffects();
        second.ClearEffects();

        _turn = StartTurn(1);
    }

    public Character First => _fighters[0];

    public Character Second => _fighters[1];

    public Character CurrentActor => _turn.CurrentActor;

    public Character CurrentOpponent => _turn.CurrentOpponent;

    /// <summary>The current turn (round). Starts at 1.</summary>
    public int Round => _turn.Number;

    /// <summary>The turn that is being played right now.</summary>
    public Turn CurrentTurn => _turn;

    /// <summary>All turns so far, the current one last.</summary>
    public IReadOnlyList<Turn> Turns => _turns;

    /// <summary>Every action performed so far, in order: the history of the battle.</summary>
    public IReadOnlyList<TurnEntry> History => [.. _turns.SelectMany(t => t.Entries)];

    /// <summary>How many actions have been played so far.</summary>
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

        _turn.Act(action, TurnsPlayed + 1);
        TurnsPlayed++;

        if (IsFinished) return;

        if (_turn.IsComplete)
            _turn = StartTurn(_turn.Number + 1);
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

    private Turn StartTurn(int number)
    {
        var turn = new Turn(number, _fighters);
        _turns.Add(turn);
        turn.BeginTurn();
        return turn;
    }

    private static GameConfig Require(GameConfig config)
        => config ?? throw new ArgumentNullException(nameof(config));
}
