using System.Collections.Concurrent;
using GameSystem.Presentation;
using GameSystem.Presentation.Console;
using GameSystem.Tests.TestDoubles;

namespace GameSystem.Tests.Gui;

/// <summary>
/// The game as the window runs it, but with in-memory storage and a scripted opponent: the game thread is real,
/// so a test submits a line and then waits for the snapshot that the game publishes when it asks for the next one.
/// </summary>
internal sealed class HostHarness : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly BlockingCollection<GameSnapshot> _snapshots = new();
    private readonly List<ConsoleLine> _lines = [];

    public HostHarness(InMemoryGameStateRepository? states = null, InMemoryGameResultRepository? results = null, bool start = true, FakeGenshinApiClient? api = null)
    {
        States = states ?? new InMemoryGameStateRepository();
        Results = results ?? new InMemoryGameResultRepository();
        Console = new GuiConsole();
        Console.LineWritten += line => { lock (_lines) _lines.Add(line); };

        Host = new GameHost(Console, States, Results, apiClient: api, opponentStrategy: new AlwaysDefendStrategy(), clock: () => new DateTime(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc));
        Host.SnapshotReady += _snapshots.Add;

        if (start)
            Host.Start();
    }

    public GuiConsole Console { get; }

    public GameHost Host { get; }

    public InMemoryGameStateRepository States { get; }

    public InMemoryGameResultRepository Results { get; }

    public IReadOnlyList<ConsoleLine> Lines
    {
        get
        {
            lock (_lines) return [.. _lines];
        }
    }

    /// <summary>Waits for the first snapshot that satisfies the condition; older ones are skipped.</summary>
    public GameSnapshot WaitFor(Func<GameSnapshot, bool> condition)
    {
        var deadline = DateTime.UtcNow + Timeout;

        while (true)
        {
            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero || !_snapshots.TryTake(out var snapshot, remaining))
                throw new TimeoutException("The game did not reach the expected state in time.");

            if (condition(snapshot))
                return snapshot;
        }
    }

    /// <summary>Types every line in turn, as a user would, one after the game asks for it.</summary>
    public void Type(params string[] lines)
    {
        foreach (string line in lines)
            Console.Submit(line);
    }

    public void Dispose() => Host.Dispose();
}
