using GameSystem.FileManager.Editing;
using GameSystem.FileManager.Files;
using GameSystem.FileManager.Rendering;

namespace GameSystem.FileManager.Modes;

/// <summary>
/// Mode 3: text editing. On top of the usual navigation keys this mode captures typed characters
/// and turns them into changes of the <see cref="TextBuffer"/>.
/// </summary>
public sealed class EditMode : AppModeBase
{
    private static readonly (string Key, string Action)[] KeyHints =
    [
        ("←↑↓→", "move"),
        ("Ctrl+S / F2", "save"),
        ("Esc", "back to view"),
    ];

    private readonly FileEntry _file;
    private readonly string _directory;
    private readonly TextBuffer _buffer;
    private readonly Viewport _viewport = new();
    private IReadOnlyList<string> _savedLines;
    private int _leftColumn;
    private bool _discardArmed;

    public EditMode(IAppContext context, FileEntry file, string directory, IReadOnlyList<string> lines)
        : base(context)
    {
        _file = file;
        _directory = directory;
        _savedLines = lines;
        _buffer = new TextBuffer(lines);

        Bind(ConsoleKey.LeftArrow, _buffer.MoveLeft);
        Bind(ConsoleKey.RightArrow, _buffer.MoveRight);
        Bind(ConsoleKey.UpArrow, _buffer.MoveUp);
        Bind(ConsoleKey.DownArrow, _buffer.MoveDown);
        Bind(ConsoleKey.Home, _buffer.MoveHome);
        Bind(ConsoleKey.End, _buffer.MoveEnd);
        Bind(ConsoleKey.PageUp, () => _buffer.MoveVertically(-Context.BodyHeight));
        Bind(ConsoleKey.PageDown, () => _buffer.MoveVertically(Context.BodyHeight));
        Bind(ConsoleKey.Enter, _buffer.InsertNewLine);
        Bind(ConsoleKey.Backspace, _buffer.Backspace);
        Bind(ConsoleKey.Delete, _buffer.Delete);
        Bind(ConsoleKey.Tab, () => _buffer.InsertText(new string(' ', TextSanitizer.TabWidth)));
        Bind(ConsoleKey.S, Save, ConsoleModifiers.Control);
        Bind(ConsoleKey.F2, Save);
        Bind(ConsoleKey.Escape, ExitEditing);
    }

    public override string Name => "Edit";

    protected override Style BadgeStyle => Palette.EditBadge;

    // Typed characters belong to the text here, so ":" must not open the command bar.
    protected override bool SupportsCommandBar => false;

    protected override IReadOnlyList<(string Key, string Action)> Hints => KeyHints;

    public override void HandleKey(ConsoleKeyInfo key)
    {
        // Any key other than Esc cancels a pending "discard changes" confirmation.
        if (key.Key != ConsoleKey.Escape)
        {
            _discardArmed = false;
        }

        base.HandleKey(key);
    }

    protected override void OnUnboundKey(ConsoleKeyInfo key)
    {
        if (!char.IsControl(key.KeyChar))
        {
            _buffer.Insert(key.KeyChar);
        }
    }

    protected override StyledLine BuildSubtitle()
    {
        var line = new StyledLine().Add(" ").Add(_file.FullPath, Palette.Path);
        return _buffer.IsDirty
            ? line.Add("  ● modified", Palette.Modified)
            : line.Add("  ✔ saved", Palette.Saved);
    }

    protected override void RenderBody(Canvas canvas)
    {
        var height = canvas.BodyHeight;
        var lines = _buffer.Lines;
        var gutter = LineGutter.Width(lines.Count);
        var textWidth = Math.Max(1, canvas.Width - gutter);

        _viewport.EnsureVisible(_buffer.Row, height, lines.Count);
        if (_buffer.Column < _leftColumn)
        {
            _leftColumn = _buffer.Column;
        }
        else if (_buffer.Column >= _leftColumn + textWidth)
        {
            _leftColumn = _buffer.Column - textWidth + 1;
        }

        var last = Math.Min(lines.Count, _viewport.Top + height);
        for (var i = _viewport.Top; i < last; i++)
        {
            var isCurrent = i == _buffer.Row;
            var flat = TextSanitizer.Flatten(lines[i]);
            var visible = flat.Length > _leftColumn
                ? flat.Substring(_leftColumn, Math.Min(textWidth, flat.Length - _leftColumn))
                : string.Empty;

            // The current line is padded so its background reaches the right edge.
            canvas.Body.Add(LineGutter.Create(i + 1, lines.Count, isCurrent)
                .Add(isCurrent ? visible.PadRight(textWidth) : visible, isCurrent ? Palette.CurrentLine : Palette.Text));
        }

        canvas.Cursor = (_buffer.Row - _viewport.Top, gutter + _buffer.Column - _leftColumn);
    }

    protected override StyledLine? BuildDefaultStatus() => new StyledLine(
        $" Ln {_buffer.Row + 1}, Col {_buffer.Column + 1}  ·  {_buffer.Lines.Count} lines",
        Palette.Dim);

    private void Save()
    {
        try
        {
            var snapshot = _buffer.Lines.ToList();
            Context.FileSystem.WriteLines(_file.FullPath, snapshot);
            _savedLines = snapshot;
            _buffer.MarkSaved();
            ShowSuccess($"Saved {_file.Name} ({snapshot.Count} lines)");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError($"Cannot save {_file.Name}: {ex.Message}");
        }
    }

    private void ExitEditing()
    {
        if (_buffer.IsDirty && !_discardArmed)
        {
            _discardArmed = true;
            ShowError("Unsaved changes! Press Esc again to discard them, or Ctrl+S to save.");
            return;
        }

        // Back to the view mode, showing what is actually stored in the file.
        Context.ChangeMode(new FileViewMode(Context, _file, _directory, _savedLines));
    }
}
