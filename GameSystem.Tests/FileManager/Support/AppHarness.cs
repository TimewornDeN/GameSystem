using GameSystem.CLI;
using GameSystem.FileManager.App;
using GameSystem.FileManager.Modes;
using GameSystem.Tests.TestDoubles;

namespace GameSystem.Tests.FileManager.Support;

/// <summary>
/// Wires the application to the fakes. The default disk is:
/// /root { docs/, locked/ (unreadable), notes.txt, readme.md } and /root/docs { todo.txt }.
/// In /root the selection order is: docs, locked, notes.txt, readme.md.
/// </summary>
internal sealed class AppHarness
{
    public AppHarness(Action<InMemoryFileSystem>? extraSetup = null)
    {
        Files.AddDirectory("/root");
        Files.AddDirectory("/root/docs", "/root");
        Files.AddDirectory("/root/locked", "/root", locked: true);
        Files.AddFile("/root", "notes.txt", "alpha", "beta");
        Files.AddFile("/root", "readme.md", "# Title", "second line");
        Files.AddFile("/root/docs", "todo.txt", "buy milk");
        extraSetup?.Invoke(Files);

        App = new FileManagerApp(Terminal, Files, new CommandParser(), "/root");
    }

    public FakeTerminal Terminal { get; } = new();

    public InMemoryFileSystem Files { get; } = new();

    public FileManagerApp App { get; }

    /// <summary>The currently visible text without any colors.</summary>
    public string Screen
    {
        get
        {
            App.Render();
            return TestText.StripAnsi(Terminal.LastFrame);
        }
    }

    public void Send(params ConsoleKeyInfo[] keys)
    {
        foreach (var key in keys)
        {
            Terminal.Enqueue(key);
            App.Step();
        }
    }

    /// <summary>Types text as if the user pressed one printable key per character.</summary>
    public void Type(string text) => Send(text.Select(Keys.Text).ToArray());

    public void OpenNotes() => Send(Keys.Down, Keys.Down, Keys.Enter);

    public void OpenNotesInEditor()
    {
        OpenNotes();
        Send(Keys.E);
    }

    public bool IsIn<TMode>() where TMode : IAppMode => App.CurrentMode is TMode;
}
