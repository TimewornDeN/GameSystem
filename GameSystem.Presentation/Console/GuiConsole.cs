using System.Collections.Concurrent;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Logging;

namespace GameSystem.Presentation.Console;

/// <summary>
/// The game window used as a console. It plays all three roles that the console played before:
/// it shows messages (<see cref="IDisplayer"/>), shows the narration of a fight (<see cref="ICombatLogger"/>) and
/// hands the lines typed by the user to the game (<see cref="IInputReader"/>).
/// The game loop runs on its own thread and blocks in <see cref="ReadLine"/> until the window submits a line.
/// </summary>
public sealed class GuiConsole : IDisplayer, ICombatLogger, IInputReader
{
    private readonly BlockingCollection<string> _lines = new();
    private readonly CancellationTokenSource _closed = new();
    private volatile string _prompt = "> ";

    /// <summary>Raised (on the thread that writes) for every line that must appear in the output field.</summary>
    public event Action<ConsoleLine>? LineWritten;

    /// <summary>Raised on the game thread when the game starts to wait for a line; the argument is the prompt.</summary>
    public event Action<string>? WaitingForInput;

    public bool IsClosed => _closed.IsCancellationRequested;

    public void Display(IDisplayable item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Write(new ConsoleLine(Prefix(item.Kind) + item.Text, StyleOf(item.Kind)));
    }

    public void Log(string message, LogColor color = LogColor.Default)
        => Write(new ConsoleLine(message, StyleOf(color)));

    public string? ReadLine(string? prompt = null)
    {
        _prompt = string.IsNullOrEmpty(prompt) ? "> " : prompt;
        WaitingForInput?.Invoke(_prompt);

        try
        {
            return _lines.Take(_closed.Token);
        }
        catch (OperationCanceledException)
        {
            // The window was closed: the input is over, exactly like the end of a redirected console input.
            return null;
        }
    }

    /// <summary>Gives the game a line typed (or produced by a button) in the window. Ignored after <see cref="Close"/>.</summary>
    public void Submit(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (IsClosed) return;

        Write(new ConsoleLine(_prompt + line, LineStyle.Echo));
        _lines.Add(line);
    }

    /// <summary>Ends the input: a game that waits for a line gets null and stops.</summary>
    public void Close() => _closed.Cancel();

    private void Write(ConsoleLine line) => LineWritten?.Invoke(line);

    private static string Prefix(DisplayKind kind) => kind switch
    {
        DisplayKind.Info => "[Info]: ",
        DisplayKind.Success => "[Success]: ",
        DisplayKind.Warning => "[Warning]: ",
        DisplayKind.Error => "[Error]: ",
        _ => string.Empty
    };

    private static LineStyle StyleOf(DisplayKind kind) => kind switch
    {
        DisplayKind.Title => LineStyle.Title,
        DisplayKind.Info => LineStyle.Info,
        DisplayKind.Success => LineStyle.Success,
        DisplayKind.Warning => LineStyle.Warning,
        DisplayKind.Error => LineStyle.Error,
        _ => LineStyle.Plain
    };

    private static LineStyle StyleOf(LogColor color) => color switch
    {
        LogColor.Red or LogColor.DarkRed => LineStyle.Damage,
        LogColor.Green => LineStyle.Heal,
        LogColor.Cyan => LineStyle.Buff,
        LogColor.Magenta => LineStyle.Magic,
        LogColor.Gray => LineStyle.Dim,
        LogColor.Yellow or LogColor.DarkYellow => LineStyle.Gold,
        _ => LineStyle.Plain
    };
}
