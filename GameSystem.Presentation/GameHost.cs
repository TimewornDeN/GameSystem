using GameSystem.CLI;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Engine;
using GameSystem.Core.Management;
using GameSystem.Presentation.Catalog;
using GameSystem.Presentation.Console;

namespace GameSystem.Presentation;

/// <summary>
/// Runs the game for the window. It builds the same command interface as the console program
/// (<see cref="CliFactory"/>) but plugs the <see cref="GuiConsole"/> into every port, then runs the usual
/// "read a line, execute it" loop on its own thread, so the window never freezes while the game thinks or waits.
/// </summary>
public sealed class GameHost : IDisposable
{
    public const string MainPrompt = "> ";

    private readonly GuiConsole _console;
    private readonly IGameStateRepository _states;
    private readonly Cli _cli;
    private readonly string? _welcomeNote;
    private readonly ICharacterCatalogStore? _catalogStore;
    private IReadOnlyList<ServerCharacterCard> _serverCatalog = [];
    private bool _catalogDirty = true;
    private Thread? _thread;

    public GameHost(
        GuiConsole console,
        IGameStateRepository states,
        IGameResultRepository results,
        IGenshinApiClient? apiClient = null,
        ICharacterProfileSource? profiles = null,
        ITurnStrategy? opponentStrategy = null,
        Func<DateTime>? clock = null,
        string? welcomeNote = null,
        ICharacterCatalogStore? catalogStore = null,
        IPortraitProvider? portraits = null)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _states = states ?? throw new ArgumentNullException(nameof(states));
        _welcomeNote = welcomeNote;
        _catalogStore = catalogStore;

        World = new GameWorld();
        _cli = CliFactory.Create(
            displayer: console,
            input: console,
            logger: console,
            states: states,
            results: results,
            apiClient: apiClient,
            world: World,
            opponentStrategy: opponentStrategy,
            clock: clock,
            profiles: profiles,
            catalogStore: catalogStore,
            portraits: portraits);

        _console.WaitingForInput += OnWaitingForInput;
    }

    /// <summary>The world of the running game. Only the game thread may touch it.</summary>
    public GameWorld World { get; }

    /// <summary>Raised on the game thread each time the game starts to wait for a line.</summary>
    public event Action<GameSnapshot>? SnapshotReady;

    /// <summary>Starts the game loop on its own background thread.</summary>
    public void Start()
    {
        if (_thread is not null)
            throw new InvalidOperationException("The game is already running.");

        _thread = new Thread(Loop) { IsBackground = true, Name = "GameSystem game loop" };
        _thread.Start();
    }

    /// <summary>Stops the input and waits a moment for the game thread to finish.</summary>
    public void Dispose()
    {
        _console.Close();
        _thread?.Join(TimeSpan.FromSeconds(2));
        _console.WaitingForInput -= OnWaitingForInput;
    }

    /// <summary>Blocks until the game loop has ended (it ends after <see cref="Dispose"/> or when the input is closed).</summary>
    public bool WaitForExit(TimeSpan timeout) => _thread?.Join(timeout) ?? true;

    private void Loop()
    {
        _console.Show("--- GameSystem started. Type 'help' for the commands. ---", DisplayKind.Title);
        if (_welcomeNote is not null)
            _console.Info(_welcomeNote);
        if (SafeLatest() is { } last)
            _console.Info($"Last save: '{last}'. Press Continue (or type 'continue') to go on from it.");

        while (true)
        {
            string? line = _console.ReadLine(MainPrompt);
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                _console.Info("Close the window to exit the game.");
                continue;
            }

            // The 'catalog' command may change the saved catalog: the next snapshot reads it again.
            if (line.TrimStart().StartsWith("catalog", StringComparison.OrdinalIgnoreCase))
                _catalogDirty = true;

            try
            {
                _cli.ExecCommand(line);
            }
            catch (Exception ex)
            {
                // The loop must survive anything: a broken command should not end the game.
                _console.Error($"Unexpected error: {ex.Message}");
            }
        }
    }

    private void OnWaitingForInput(string prompt)
    {
        GameSnapshot snapshot;
        try
        {
            snapshot = BuildSnapshot(prompt);
        }
        catch (Exception ex)
        {
            _console.Error($"Could not refresh the window: {ex.Message}");
            return;
        }

        SnapshotReady?.Invoke(snapshot);
    }

    private GameSnapshot BuildSnapshot(string prompt)
    {
        var battle = World.Battle is { Session.IsFinished: false } b ? b.Session : null;

        PromptMode mode = prompt == MainPrompt
            ? PromptMode.Main
            : battle is not null && prompt.StartsWith('[')
                ? PromptMode.Battle
                : PromptMode.Dialog;

        string summary = battle is null
            ? string.Empty
            : $"Round {battle.Round}: {battle.First.Name} ({battle.First.Health}/{battle.First.MaxHealth}) vs "
              + $"{battle.Second.Name} ({battle.Second.Health}/{battle.Second.MaxHealth}), turn of {battle.CurrentActor.Name}";

        return new GameSnapshot(CharacterCard.BuildAll(World), SafeList(), SafeLatest(), mode, prompt, summary, LoadServerCatalog());
    }

    /// <summary>The saved catalog of the server's characters; it is read from the disk only when it may have changed.</summary>
    private IReadOnlyList<ServerCharacterCard> LoadServerCatalog()
    {
        if (_catalogStore is null)
            return [];

        if (!_catalogDirty)
            return _serverCatalog;

        _catalogDirty = false;
        try
        {
            _serverCatalog = [.. _catalogStore.Load().Select(ServerCharacterCard.From)];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _serverCatalog = [];
        }

        return _serverCatalog;
    }

    private IReadOnlyList<string> SafeList()
    {
        try { return _states.List(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return []; }
    }

    private string? SafeLatest()
    {
        try { return _states.Latest(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return null; }
    }
}
