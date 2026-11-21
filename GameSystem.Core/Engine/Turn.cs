using GameSystem.Core.Characters;
using GameSystem.Core.Combat.Actions;

namespace GameSystem.Core.Engine;

/// <summary>
/// One step of the battle: every fighter acts as many times as its speed allows, in the order of its speed.
/// <list type="bullet">
/// <item>Speed 100 (the slowest fighter) means one action.</item>
/// <item>A faster fighter gets <c>floor(speed / slowestSpeed)</c> actions per turn (at least one).</item>
/// <item>The k-th of n actions of a fighter happens at the moment k/n of the turn. Within the same moment the faster
/// fighter goes first, equal speed keeps the order in which the fighters were given.</item>
/// </list>
/// So speed 200 against 100 gives the order A, B, A; speed 150 against 100 gives A, B.
/// </summary>
public sealed class Turn
{
    private readonly List<Character> _order = [];
    private readonly List<TurnEntry> _entries = [];
    private readonly IReadOnlyList<Character> _fighters;
    private bool _started;

    public Turn(int number, IReadOnlyList<Character> fighters)
    {
        ArgumentNullException.ThrowIfNull(fighters);

        if (number < 1)
            throw new ArgumentOutOfRangeException(nameof(number), "Turn numbers start at 1.");

        if (fighters.Count != 2)
            throw new ArgumentException("A turn needs exactly two fighters.", nameof(fighters));

        Number = number;
        _fighters = [.. fighters];
    }

    /// <summary>The round number, starting at 1.</summary>
    public int Number { get; }

    /// <summary>All actions of this turn in the order they will happen (the same fighter may appear twice).</summary>
    public IReadOnlyList<Character> Order => _order;  // empty until BeginTurn

    /// <summary>Index into <see cref="Order"/> of the action that is next.</summary>
    public int Position { get; private set; }

    public bool IsComplete => Position >= _order.Count;

    /// <summary>The fighter whose action is next (after the last action: the one who acted last).</summary>
    public Character CurrentActor
    {
        get
        {
            if (!_started)
                throw new InvalidOperationException("The turn has not begun. Call BeginTurn first.");

            return _order[Math.Min(Position, _order.Count - 1)];
        }
    }

    public Character CurrentOpponent => OpponentOf(CurrentActor);

    /// <summary>What happened in this turn so far.</summary>
    public IReadOnlyList<TurnEntry> Entries => _entries;

    /// <summary>
    /// Starts the turn: the temporary effects of both fighters wear off by one turn, and only then the order of actions
    /// is fixed, so a speed buff that has just expired no longer counts. Does nothing when called again.
    /// </summary>
    public void BeginTurn()
    {
        if (_started) return;
        _started = true;

        foreach (var fighter in _fighters)
            fighter.TickEffects();

        _order.AddRange(BuildOrder(_fighters));
    }

    /// <summary>
    /// Performs one action of <see cref="CurrentActor"/> and moves on. When the action throws, nothing is recorded
    /// and the same fighter is still next.
    /// </summary>
    public TurnEntry Act(ICombatAction action, int sequence)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!_started)
            throw new InvalidOperationException("The turn has not begun. Call BeginTurn first.");

        if (IsComplete)
            throw new InvalidOperationException("The turn is already complete.");

        var actor = CurrentActor;
        var opponent = OpponentOf(actor);
        int actorBefore = actor.Health;
        int opponentBefore = opponent.Health;

        action.Execute();
        Position++;

        var entry = new TurnEntry(
            Number, sequence, actor.Name, opponent.Name, action.Name,
            actorBefore, actor.Health, opponentBefore, opponent.Health);
        _entries.Add(entry);

        return entry;
    }

    /// <summary>
    /// Rebuilds a turn that was saved half-way: the order of actions is taken as it was (it cannot be recomputed,
    /// because the effects have already ticked), the entries are what has happened so far and the position follows from them.
    /// </summary>
    /// <param name="order">Indices (0 or 1) into <paramref name="fighters"/>, one per action of the turn.</param>
    /// <exception cref="ArgumentException">The saved data is inconsistent.</exception>
    public static Turn Restore(int number, IReadOnlyList<Character> fighters, IReadOnlyList<int> order, IEnumerable<TurnEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(entries);

        var turn = new Turn(number, fighters);

        if (order.Count == 0)
            throw new ArgumentException("A turn has at least one action.", nameof(order));

        foreach (int index in order)
        {
            if (index is < 0 or > 1)
                throw new ArgumentException($"Unknown fighter index {index} in the order of actions.", nameof(order));

            turn._order.Add(turn._fighters[index]);
        }

        turn._entries.AddRange(entries);

        if (turn._entries.Count > turn._order.Count)
            throw new ArgumentException($"Turn {number} has more entries than actions.", nameof(entries));

        turn.Position = turn._entries.Count;
        turn._started = true;
        return turn;
    }

    private Character OpponentOf(Character actor)
        => ReferenceEquals(actor, _fighters[0]) ? _fighters[1] : _fighters[0];

    private static List<Character> BuildOrder(IReadOnlyList<Character> fighters)
    {
        int slowest = fighters.Min(f => f.EffectiveSpeed);
        var moments = new List<(double Time, int Speed, int Index, Character Fighter)>();

        for (int i = 0; i < fighters.Count; i++)
        {
            int speed = fighters[i].EffectiveSpeed;
            int actions = Math.Max(1, speed / slowest);

            for (int k = 0; k < actions; k++)
                moments.Add((k / (double)actions, speed, i, fighters[i]));
        }

        return [.. moments
            .OrderBy(m => m.Time)
            .ThenByDescending(m => m.Speed)
            .ThenBy(m => m.Index)
            .Select(m => m.Fighter)];
    }
}
