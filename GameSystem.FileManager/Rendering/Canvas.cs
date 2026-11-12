using System.Text;

namespace GameSystem.FileManager.Rendering;

/// <summary>
/// Virtual screen. It is split into a header (title + subtitle), a scrollable body and a footer
/// (status + key hints). Modes only fill the segments; the canvas turns them into one ANSI frame.
/// </summary>
public sealed class Canvas
{
    /// <summary>Rows taken by the header and the footer together.</summary>
    public const int ChromeRows = 4;

    private static readonly StyledLine EmptyLine = new();

    public Canvas(int width, int height)
    {
        Width = Math.Max(20, width);
        Height = Math.Max(ChromeRows + 1, height);
    }

    public int Width { get; }

    public int Height { get; }

    public int BodyHeight => Height - ChromeRows;

    public StyledLine Title { get; set; } = new();

    public StyledLine Subtitle { get; set; } = new();

    public List<StyledLine> Body { get; } = [];

    public StyledLine Status { get; set; } = new();

    public StyledLine Hints { get; set; } = new();

    /// <summary>Cursor position inside the body (zero-based). When <c>null</c> the cursor is hidden.</summary>
    public (int Row, int Column)? Cursor { get; set; }

    /// <summary>Builds the complete frame. Every row is positioned explicitly, so nothing ever scrolls.</summary>
    public string Compose()
    {
        var builder = new StringBuilder();
        builder.Append(Ansi.HideCursor);

        AppendRow(builder, 1, Title, Palette.TitleBar);
        AppendRow(builder, 2, Subtitle, default);

        for (var i = 0; i < BodyHeight; i++)
        {
            AppendRow(builder, 3 + i, i < Body.Count ? Body[i] : EmptyLine, default);
        }

        AppendRow(builder, Height - 1, Status, default);
        AppendRow(builder, Height, Hints, default);

        if (Cursor is { } cursor)
        {
            builder.Append(Ansi.MoveTo(3 + cursor.Row, cursor.Column + 1)).Append(Ansi.ShowCursor);
        }

        return builder.ToString();
    }

    private void AppendRow(StringBuilder builder, int row, StyledLine line, Style padStyle) =>
        builder.Append(Ansi.MoveTo(row, 1)).Append(line.ToAnsi(Width, padStyle));
}
