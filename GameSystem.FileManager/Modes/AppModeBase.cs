using GameSystem.CLI;
using GameSystem.CLI.Interfaces;
using GameSystem.Core.Abstractions;
using GameSystem.FileManager.Editing;
using GameSystem.FileManager.Rendering;

namespace GameSystem.FileManager.Modes;

/// <summary>
/// Shared skeleton of all modes: the frame layout (Template Method), the key map (key → action)
/// and a ":" command bar. The command bar reuses the CLI module: the typed line goes through the same
/// <see cref="ICommandParser"/> / <see cref="CommandRegistry"/> as in GameSystem.CLI, and all feedback
/// travels through <see cref="IDisplayer"/> (here a displayer that fills the status line).
/// </summary>
public abstract class AppModeBase : IAppMode
{
    private static readonly (string Key, string Action)[] CommandBarHints =
    [
        ("Enter", "run"),
        ("Esc", "cancel"),
    ];

    private readonly Dictionary<KeyChord, Action> _bindings = new();
    private readonly StatusLine _status = new();
    private TextBuffer? _commandInput;

    protected AppModeBase(IAppContext context)
    {
        Context = context;

        // Every mode has its own registry: the same command name may do different things in different modes.
        Commands = new CommandRegistry(context.CommandParser, _status);
        Commands.RegisterCommand(new RelayCommand("help", "Lists the commands of the current mode",
            _ => Feedback.Info("Commands: " + string.Join(", ", Commands.GetCommands().Select(command => command.Name)))));
        Commands.RegisterCommand(new RelayCommand("quit", "Leaves the application", _ => Context.Exit()));
    }

    public abstract string Name { get; }

    protected IAppContext Context { get; }

    protected ICommandRegistry Commands { get; }

    /// <summary>Where commands and the mode itself report results (shown in the status line).</summary>
    protected IDisplayer Feedback => _status;

    protected abstract Style BadgeStyle { get; }

    protected abstract IReadOnlyList<(string Key, string Action)> Hints { get; }

    /// <summary>Modes that capture typed characters (the editor) turn the ":" command bar off.</summary>
    protected virtual bool SupportsCommandBar => true;

    protected abstract StyledLine BuildSubtitle();

    protected abstract void RenderBody(Canvas canvas);

    /// <summary>Text shown in the status line when there is no message to display.</summary>
    protected virtual StyledLine? BuildDefaultStatus() => null;

    public void Render(Canvas canvas)
    {
        canvas.Title = new StyledLine()
            .Add(" ◆ GameSystem Files ", Palette.AppName)
            .Add(" ", Palette.TitleBar)
            .Add($" {Name.ToUpperInvariant()} ", BadgeStyle);
        canvas.Subtitle = BuildSubtitle();
        RenderBody(canvas);
        canvas.Status = BuildStatus();
        canvas.Hints = BuildHints();

        if (_commandInput is { } input)
        {
            // The status row sits directly below the body, so it is body row number BodyHeight.
            canvas.Cursor = (canvas.BodyHeight, 1 + input.Column);
        }
    }

    public virtual void HandleKey(ConsoleKeyInfo key)
    {
        if (_commandInput is not null)
        {
            HandleCommandBarKey(key);
            return;
        }

        _status.Clear();

        if (SupportsCommandBar && key.KeyChar == ':')
        {
            _commandInput = new TextBuffer([]);
            return;
        }

        // Shift is ignored on purpose, so "E" and "e" are the same command.
        var chord = new KeyChord(key.Key, key.Modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt));
        if (_bindings.TryGetValue(chord, out var action))
        {
            action();
        }
        else
        {
            OnUnboundKey(key);
        }
    }

    protected void Bind(ConsoleKey key, Action action, ConsoleModifiers modifiers = 0) =>
        _bindings[new KeyChord(key, modifiers)] = action;

    /// <summary>Called for keys without a binding (the edit mode uses it for typed characters).</summary>
    protected virtual void OnUnboundKey(ConsoleKeyInfo key)
    {
    }

    protected void ShowInfo(string text) => Feedback.Info(text);

    protected void ShowSuccess(string text) => Feedback.Success(text);

    protected void ShowError(string text) => Feedback.Error(text);

    private void HandleCommandBarKey(ConsoleKeyInfo key)
    {
        var input = _commandInput!;
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _commandInput = null;
                break;
            case ConsoleKey.Enter:
                var line = input.Lines[0];
                _commandInput = null;
                _status.Clear();
                Commands.ExecuteCommand(line);
                break;
            case ConsoleKey.Backspace when input.Lines[0].Length == 0:
                _commandInput = null; // Backspace on an empty bar closes it.
                break;
            case ConsoleKey.Backspace:
                input.Backspace();
                break;
            case ConsoleKey.Delete:
                input.Delete();
                break;
            case ConsoleKey.LeftArrow:
                input.MoveLeft();
                break;
            case ConsoleKey.RightArrow:
                input.MoveRight();
                break;
            case ConsoleKey.Home:
                input.MoveHome();
                break;
            case ConsoleKey.End:
                input.MoveEnd();
                break;
            default:
                if (!char.IsControl(key.KeyChar))
                {
                    input.Insert(key.KeyChar);
                }

                break;
        }
    }

    private StyledLine BuildStatus()
    {
        if (_commandInput is { } input)
        {
            return new StyledLine().Add(":", Palette.HintKey).Add(input.Lines[0], Palette.Text);
        }

        if (_status.Current is { } item)
        {
            // DisplayKind is the existing "style hint" of the displayer abstraction; here it picks a color.
            var (icon, style) = item.Kind switch
            {
                DisplayKind.Info => ("ℹ ", Palette.Info),
                DisplayKind.Success => ("✔ ", Palette.Success),
                DisplayKind.Warning => ("⚠ ", Palette.Warning),
                DisplayKind.Error => ("✖ ", Palette.Error),
                DisplayKind.Title => (string.Empty, Palette.HintKey),
                _ => (string.Empty, Palette.Text),
            };
            return new StyledLine().Add($" {icon}{item.Text}", style);
        }

        return BuildDefaultStatus() ?? new StyledLine();
    }

    private StyledLine BuildHints()
    {
        var line = new StyledLine().Add(" ");
        foreach (var (key, action) in _commandInput is null ? Hints : CommandBarHints)
        {
            line.Add(key, Palette.HintKey).Add($" {action}   ", Palette.HintText);
        }

        return line;
    }

    private readonly record struct KeyChord(ConsoleKey Key, ConsoleModifiers Modifiers);

    /// <summary>An <see cref="IDisplayer"/> that remembers the last item, which the mode shows in its status line.</summary>
    private sealed class StatusLine : IDisplayer
    {
        public IDisplayable? Current { get; private set; }

        public void Display(IDisplayable item)
        {
            ArgumentNullException.ThrowIfNull(item);
            Current = item;
        }

        public void Clear() => Current = null;
    }
}
