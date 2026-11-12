using GameSystem.CLI;
using GameSystem.Core.Abstractions;
using GameSystem.FileManager.Editing;
using GameSystem.FileManager.Files;
using GameSystem.FileManager.Rendering;

namespace GameSystem.FileManager.Modes;

/// <summary>
/// Mode 2: read-only view of a file. Markdown files open as a formatted document built with the Text module
/// (Root/Heading/Paragraph/Link rendered by <see cref="StyledTextVisitor"/>); "M" switches to the raw text.
/// From here the user can close the file or start editing.
/// </summary>
public sealed class FileViewMode : AppModeBase
{
    private readonly (string Key, string Action)[] _hints;
    private readonly FileEntry _file;
    private readonly string _directory;
    private readonly IReadOnlyList<string> _lines;
    private readonly Viewport _viewport = new();
    private readonly bool _isMarkdown;
    private bool _isFormatted;
    private IReadOnlyList<StyledLine>? _formatted;
    private int _formattedWidth;
    private int _lastWidth = 80;

    public FileViewMode(IAppContext context, FileEntry file, string directory, IReadOnlyList<string> lines)
        : base(context)
    {
        _file = file;
        _directory = directory;
        _lines = lines;
        _isMarkdown = string.Equals(Path.GetExtension(file.Name), ".md", StringComparison.OrdinalIgnoreCase);
        _isFormatted = _isMarkdown;

        _hints = _isMarkdown
            ? new (string Key, string Action)[] { ("↑↓ PgUp PgDn", "scroll"), ("M", "raw/formatted"), ("E", "edit"), (":", "command"), ("Q/Esc", "close") }
            : new (string Key, string Action)[] { ("↑↓ PgUp PgDn", "scroll"), ("Home/End", "jump"), ("E", "edit"), (":", "command"), ("Q/Esc", "close") };

        Bind(ConsoleKey.UpArrow, () => Scroll(-1));
        Bind(ConsoleKey.DownArrow, () => Scroll(1));
        Bind(ConsoleKey.PageUp, () => Scroll(-Context.BodyHeight));
        Bind(ConsoleKey.PageDown, () => Scroll(Context.BodyHeight));
        Bind(ConsoleKey.Home, () => _viewport.ScrollTo(0, Context.BodyHeight, DisplayCount));
        Bind(ConsoleKey.End, () => _viewport.ScrollTo(int.MaxValue, Context.BodyHeight, DisplayCount));
        Bind(ConsoleKey.M, ToggleFormatting);
        Bind(ConsoleKey.E, StartEditing);
        Bind(ConsoleKey.Q, CloseFile);
        Bind(ConsoleKey.Escape, CloseFile);

        RegisterCommands();
    }

    public override string Name => "View";

    protected override Style BadgeStyle => Palette.ViewBadge;

    protected override IReadOnlyList<(string Key, string Action)> Hints => _hints;

    /// <summary>Number of rows of whatever is shown right now: raw lines or the formatted document.</summary>
    private int DisplayCount => _isFormatted ? GetFormatted(_lastWidth).Count : _lines.Count;

    protected override StyledLine BuildSubtitle() => new StyledLine()
        .Add(" ")
        .Add(_file.FullPath, Palette.Path)
        .Add($"  {_lines.Count} lines", Palette.Dim)
        .Add(_isFormatted ? "  · formatted" : string.Empty, Palette.Saved);

    protected override void RenderBody(Canvas canvas)
    {
        _lastWidth = canvas.Width;
        var height = canvas.BodyHeight;
        var total = DisplayCount;
        _viewport.Clamp(height, total);
        var last = Math.Min(total, _viewport.Top + height);

        if (_isFormatted)
        {
            var document = GetFormatted(canvas.Width);
            for (var i = _viewport.Top; i < last; i++)
            {
                canvas.Body.Add(document[i]);
            }

            return;
        }

        if (_lines.Count == 0)
        {
            canvas.Body.Add(new StyledLine(" (empty file)", Palette.Dim));
            return;
        }

        var textWidth = Math.Max(1, canvas.Width - LineGutter.Width(_lines.Count));
        for (var i = _viewport.Top; i < last; i++)
        {
            var text = TextSanitizer.Expand(_lines[i]);
            if (text.Length > textWidth)
            {
                text = text[..textWidth];
            }

            canvas.Body.Add(LineGutter.Create(i + 1, _lines.Count, false).Add(text, Palette.Text));
        }
    }

    protected override StyledLine? BuildDefaultStatus()
    {
        var total = DisplayCount;
        if (total == 0)
        {
            return new StyledLine(" Empty file", Palette.Dim);
        }

        var first = _viewport.Top + 1;
        var last = Math.Min(total, _viewport.Top + Context.BodyHeight);
        var percent = last * 100 / total;
        return new StyledLine($" Lines {first}-{last} of {total} ({percent}%)", Palette.Dim);
    }

    /// <summary>Builds (and caches per width) the formatted document by sending a visitor through the text tree.</summary>
    private IReadOnlyList<StyledLine> GetFormatted(int width)
    {
        if (_formatted is null || _formattedWidth != width)
        {
            var visitor = new StyledTextVisitor(width);
            MarkdownDocumentParser.Parse(_file.Name, _lines).Accept(visitor);
            _formatted = visitor.Lines;
            _formattedWidth = width;
        }

        return _formatted;
    }

    private void RegisterCommands()
    {
        Commands.RegisterCommand(new RelayCommand("goto", "goto <line> — scroll to a line",
            ctx =>
            {
                if (ctx.Args.Count == 0 || !int.TryParse(ctx.Args[0], out var line) || line < 1)
                {
                    Feedback.Error("Usage: goto <line number>");
                    return;
                }

                _viewport.ScrollTo(line - 1, Context.BodyHeight, DisplayCount);
            }));
        Commands.RegisterCommand(new RelayCommand("edit", "Switches to the editor", _ => StartEditing()));
        Commands.RegisterCommand(new RelayCommand("close", "Closes the file", _ => CloseFile()));
    }

    private void ToggleFormatting()
    {
        if (!_isMarkdown)
        {
            ShowInfo("The formatted view is available for .md files only.");
            return;
        }

        _isFormatted = !_isFormatted;
        _viewport.Reset();
    }

    private void Scroll(int delta) => _viewport.ScrollBy(delta, Context.BodyHeight, DisplayCount);

    private void StartEditing() => Context.ChangeMode(new EditMode(Context, _file, _directory, _lines));

    private void CloseFile() => Context.ChangeMode(new DirectoryMode(Context, _directory, _file.Name));
}
