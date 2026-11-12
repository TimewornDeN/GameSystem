using GameSystem.CLI;
using GameSystem.Core.Abstractions;
using GameSystem.FileManager.Editing;
using GameSystem.FileManager.Files;
using GameSystem.FileManager.Rendering;

namespace GameSystem.FileManager.Modes;

/// <summary>
/// Mode 1: shows the contents of the current directory. Entering a directory stays in this mode
/// (the "user changed the path" loop of the diagram); opening a file switches to <see cref="FileViewMode"/>.
/// </summary>
public sealed class DirectoryMode : AppModeBase
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private const string ParentName = "..";

    private static readonly (string Key, string Action)[] KeyHints =
    [
        ("↑↓", "select"),
        ("Enter", "open"),
        ("Backspace", "up"),
        (":", "command"),
        ("Esc", "quit"),
    ];

    private readonly Viewport _viewport = new();
    private string _directory;
    private List<FileEntry> _items = [];
    private int _selected;

    public DirectoryMode(IAppContext context, string directory, string? selectName = null)
        : base(context)
    {
        _directory = directory;

        if (TryLoad(directory, out var items, out var error))
        {
            _items = items;
            _selected = Math.Max(0, _items.FindIndex(entry => entry.Name == selectName));
        }
        else
        {
            ShowError(error!);
        }

        Bind(ConsoleKey.UpArrow, () => MoveSelection(-1));
        Bind(ConsoleKey.DownArrow, () => MoveSelection(1));
        Bind(ConsoleKey.PageUp, () => MoveSelection(-Context.BodyHeight));
        Bind(ConsoleKey.PageDown, () => MoveSelection(Context.BodyHeight));
        Bind(ConsoleKey.Home, () => SelectIndex(0));
        Bind(ConsoleKey.End, () => SelectIndex(_items.Count - 1));
        Bind(ConsoleKey.Enter, OpenSelected);
        Bind(ConsoleKey.RightArrow, OpenSelected);
        Bind(ConsoleKey.Backspace, GoUp);
        Bind(ConsoleKey.LeftArrow, GoUp);
        Bind(ConsoleKey.Escape, Context.Exit);
        Bind(ConsoleKey.Q, Context.Exit);

        RegisterCommands();
    }

    public override string Name => "Directory";

    protected override Style BadgeStyle => Palette.DirectoryBadge;

    protected override IReadOnlyList<(string Key, string Action)> Hints => KeyHints;

    protected override StyledLine BuildSubtitle() => new StyledLine()
        .Add(" ")
        .Add(_directory, Palette.Path)
        .Add($"  {_items.Count(entry => entry.Name != ParentName)} items", Palette.Dim);

    protected override void RenderBody(Canvas canvas)
    {
        var height = canvas.BodyHeight;
        _viewport.EnsureVisible(_selected, height, _items.Count);

        // 3 cells of marker + 11 cells of size column; the name takes the rest, so every row is full width.
        var nameWidth = Math.Max(10, canvas.Width - 14);
        var last = Math.Min(_items.Count, _viewport.Top + height);
        for (var i = _viewport.Top; i < last; i++)
        {
            canvas.Body.Add(BuildRow(_items[i], i == _selected, nameWidth));
        }
    }

    protected override StyledLine? BuildDefaultStatus() => _items.Count == 0
        ? new StyledLine(" This directory is empty.", Palette.Dim)
        : new StyledLine($" {_items[_selected].Name}  ·  {_selected + 1}/{_items.Count}", Palette.Dim);

    private static StyledLine BuildRow(FileEntry entry, bool isSelected, int nameWidth)
    {
        var nameStyle = isSelected ? Palette.Selected : entry.IsDirectory ? Palette.Directory : Palette.ForFile(entry.Name);
        var markerStyle = isSelected ? Palette.Selected : Palette.Dim;
        var sizeStyle = isSelected ? Palette.Selected : Palette.Size;
        var size = entry.IsDirectory ? "<DIR>" : SizeFormatter.Format(entry.Size);

        return new StyledLine()
            .Add(isSelected ? "›" : " ", markerStyle)
            .Add(entry.IsDirectory ? "▸ " : "· ", markerStyle)
            .Add(Fit(entry.Name, nameWidth), nameStyle)
            .Add(size.PadLeft(10) + " ", sizeStyle);
    }

    private static string Fit(string text, int width) =>
        text.Length > width ? text[..(width - 1)] + "…" : text.PadRight(width);

    /// <summary>Commands typed into the ":" bar; they reuse the CLI parser, so quotes and --options just work.</summary>
    private void RegisterCommands()
    {
        Commands.RegisterCommand(new RelayCommand("cd", "cd <path> — change directory (\"quoted paths\" allowed)",
            ctx =>
            {
                if (ctx.Args.Count == 0)
                {
                    Feedback.Error("Usage: cd <path>");
                    return;
                }

                Navigate(Context.FileSystem.ResolvePath(_directory, ctx.Args[0]), null);
            }));

        Commands.RegisterCommand(new RelayCommand("open", "open <name> [--edit] — open a file (straight into the editor with --edit) or a directory",
            ctx =>
            {
                var name = ctx.Args.FirstOrDefault();
                if (name is null)
                {
                    Feedback.Error("Usage: open <name> [--edit]");
                    return;
                }

                var entry = _items.FirstOrDefault(item =>
                    item.Name != ParentName && item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (entry is null)
                {
                    Feedback.Error($"'{name}' was not found in this directory.");
                }
                else if (entry.IsDirectory)
                {
                    Navigate(entry.FullPath, null);
                }
                else
                {
                    OpenFile(entry, ctx.HasOption("edit"));
                }
            }));
    }

    private bool TryLoad(string directory, out List<FileEntry> items, out string? error)
    {
        try
        {
            var list = new List<FileEntry>();
            var parent = Context.FileSystem.GetParent(directory);
            if (parent is not null)
            {
                list.Add(new FileEntry(ParentName, parent, true, 0));
            }

            list.AddRange(Context.FileSystem.GetEntries(directory));
            items = list;
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            items = [];
            error = $"Cannot open \"{directory}\": {ex.Message}";
            return false;
        }
    }

    private void Navigate(string directory, string? selectName)
    {
        // The old listing stays on screen if the new directory cannot be read.
        if (!TryLoad(directory, out var items, out var error))
        {
            ShowError(error!);
            return;
        }

        _directory = directory;
        _items = items;
        _viewport.Reset();
        _selected = Math.Max(0, _items.FindIndex(entry => entry.Name == selectName));
    }

    private void GoUp()
    {
        var parent = Context.FileSystem.GetParent(_directory);
        if (parent is null)
        {
            ShowInfo("Already at the root directory.");
            return;
        }

        Navigate(parent, Path.GetFileName(_directory.TrimEnd('/', '\\')));
    }

    private void OpenSelected()
    {
        if (_items.Count == 0)
        {
            return;
        }

        var entry = _items[_selected];
        if (!entry.IsDirectory)
        {
            OpenFile(entry, startInEditMode: false);
        }
        else if (entry.Name == ParentName)
        {
            GoUp();
        }
        else
        {
            Navigate(entry.FullPath, null);
        }
    }

    private void OpenFile(FileEntry entry, bool startInEditMode)
    {
        if (entry.Size > MaxFileSizeBytes)
        {
            ShowError($"{entry.Name} is too large to open ({SizeFormatter.Format(entry.Size)}).");
            return;
        }

        IReadOnlyList<string> lines;
        try
        {
            lines = Context.FileSystem.ReadLines(entry.FullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError($"Cannot read {entry.Name}: {ex.Message}");
            return;
        }

        if (startInEditMode)
        {
            Context.ChangeMode(new EditMode(Context, entry, _directory, lines));
        }
        else
        {
            Context.ChangeMode(new FileViewMode(Context, entry, _directory, lines));
        }
    }

    private void MoveSelection(int delta) => SelectIndex(_selected + delta);

    private void SelectIndex(int index) =>
        _selected = _items.Count == 0 ? 0 : Math.Clamp(index, 0, _items.Count - 1);
}
