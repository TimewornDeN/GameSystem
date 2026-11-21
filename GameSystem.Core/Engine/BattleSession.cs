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

    private BattleSession(Character first, Character second, List<Turn> turns, int turnsPlayed)
    {
        _fighters = [first, second];
        _turns.AddRange(turns);
        _turn = _turns[^1];
        TurnsPlayed = turnsPlayed;
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

    /// <summary>
    /// Captures everything that is needed to continue this battle later: whose turn it is, the order of the
    /// current round and the whole history. The state of the fighters themselves is saved with the characters.
    /// </summary>
    /// <exception cref="InvalidOperationException">The battle is already finished, there is nothing to continue.</exception>
    public BattleStateModel CaptureState(bool secondFighterIsAi)
    {
        if (IsFinished)
            throw new InvalidOperationException("A finished battle cannot be saved.");

        return new BattleStateModel(
            First.Name,
            Second.Name,
            secondFighterIsAi,
            Round,
            TurnsPlayed,
            [.. _turn.Order.Select(c => ReferenceEquals(c, First) ? 0 : 1)],
            [.. History]);
    }

    /// <summary>
    /// Continues a saved battle with the given (already restored) characters. Unlike the public constructors it
    /// does not clear effects and does not start a new turn: the battle goes on exactly where it stopped.
    /// </summary>
    /// <exception cref="ArgumentException">The saved battle does not match the characters or is inconsistent.</exception>
    public static BattleSession Restore(BattleStateModel state, IReadOnlyList<Character> characters)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(characters);

        Character Find(string name) => characters.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"The saved battle refers to an unknown character '{name}'.", nameof(state));

        var first = Find(state.FirstName);
        var second = Find(state.SecondName);

        if (ReferenceEquals(first, second))
            throw new ArgumentException("A character cannot fight itself.", nameof(state));

        if (first.Health <= 0 || second.Health <= 0)
            throw new ArgumentException("Both fighters of a saved battle must be alive.", nameof(state));

        if (state.Round < 1)
            throw new ArgumentException("Rounds start at 1.", nameof(state));

        if (state.TurnsPlayed != state.History.Count)
            throw new ArgumentException("The number of played actions does not match the history.", nameof(state));

        if (state.History.Any(e => e.Round < 1 || e.Round > state.Round))
            throw new ArgumentException("The history refers to a round that does not exist.", nameof(state));

        Character[] fighters = [first, second];

        int IndexOf(string actor)
            => actor.Equals(first.Name, StringComparison.OrdinalIgnoreCase) ? 0
                : actor.Equals(second.Name, StringComparison.OrdinalIgnoreCase) ? 1
                : throw new ArgumentException($"The history refers to an unknown actor '{actor}'.", nameof(state));

        List<Turn> turns = [];

        // Finished rounds: everybody has acted, so the order is simply who acted in the history.
        for (int round = 1; round < state.Round; round++)
        {
            var entries = state.History.Where(e => e.Round == round).ToList();
            if (entries.Count == 0)
                throw new ArgumentException($"The history of round {round} is missing.", nameof(state));

            turns.Add(Turn.Restore(round, fighters, [.. entries.Select(e => IndexOf(e.Actor))], entries));
        }

        // The round that is being played: its order was saved, because it cannot be recomputed.
        var current = state.History.Where(e => e.Round == state.Round).ToList();
        var currentTurn = Turn.Restore(state.Round, fighters, state.CurrentOrder, current);

        if (currentTurn.IsComplete)
            throw new ArgumentException("The saved round is already complete, so the battle could not be in progress.", nameof(state));

        turns.Add(currentTurn);
        return new BattleSession(first, second, turns, state.TurnsPlayed);
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
