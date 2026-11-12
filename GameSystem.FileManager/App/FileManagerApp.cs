using GameSystem.CLI.Interfaces;
using GameSystem.FileManager.Files;
using GameSystem.FileManager.Modes;
using GameSystem.FileManager.Rendering;
using GameSystem.FileManager.Terminal;

namespace GameSystem.FileManager.App;

/// <summary>
/// The State pattern context: it owns the current mode and runs the render → key → handle loop.
/// Everything that touches the outside world comes through <see cref="ITerminal"/> and <see cref="IFileSystem"/>.
/// </summary>
public sealed class FileManagerApp : IAppContext
{
    private readonly ITerminal _terminal;

    public FileManagerApp(ITerminal terminal, IFileSystem fileSystem, ICommandParser commandParser, string startDirectory)
    {
        _terminal = terminal;
        FileSystem = fileSystem;
        CommandParser = commandParser;
        CurrentMode = new DirectoryMode(this, startDirectory);
    }

    public IFileSystem FileSystem { get; }

    public ICommandParser CommandParser { get; }

    public IAppMode CurrentMode { get; private set; }

    public bool IsExitRequested { get; private set; }

    public int BodyHeight => Math.Max(1, _terminal.Height - Canvas.ChromeRows);

    public void ChangeMode(IAppMode mode) => CurrentMode = mode;

    public void Exit() => IsExitRequested = true;

    /// <summary>Runs until the user leaves the application; the terminal is always restored.</summary>
    public void Run()
    {
        _terminal.Open();
        try
        {
            while (!IsExitRequested)
            {
                Step();
            }
        }
        finally
        {
            _terminal.Close();
        }
    }

    /// <summary>One iteration of the loop: draw, wait for a key, let the current mode handle it.</summary>
    public void Step()
    {
        Render();
        CurrentMode.HandleKey(_terminal.ReadKey());
    }

    public void Render()
    {
        var canvas = new Canvas(_terminal.Width, _terminal.Height);
        CurrentMode.Render(canvas);
        _terminal.Present(canvas.Compose());
    }
}
