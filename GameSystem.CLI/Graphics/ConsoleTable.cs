namespace GameSystem.CLI.Graphics;

/// <summary>Builds an aligned text table with a header row.</summary>
public sealed class ConsoleTable
{
    private readonly string[] _headers;
    private readonly List<string[]> _rows = [];

    public ConsoleTable(params string[] headers)
    {
        if (headers is null || headers.Length == 0)
            throw new ArgumentException("A table needs at least one column.", nameof(headers));

        _headers = headers;
    }

    public int RowCount => _rows.Count;

    public ConsoleTable AddRow(params string[] cells)
    {
        if (cells.Length != _headers.Length)
            throw new ArgumentException($"Expected {_headers.Length} cells but got {cells.Length}.", nameof(cells));

        _rows.Add(cells);
        return this;
    }

    public override string ToString()
    {
        int[] widths = [.. _headers.Select((header, i) =>
            Math.Max(header.Length, _rows.Select(row => row[i].Length).DefaultIfEmpty(0).Max()))];

        string Line(string[] cells) => "| " + string.Join(" | ", cells.Select((cell, i) => cell.PadRight(widths[i]))) + " |";
        string separator = "+-" + string.Join("-+-", widths.Select(w => new string('-', w))) + "-+";

        var lines = new List<string> { separator, Line(_headers), separator };
        lines.AddRange(_rows.Select(Line));
        lines.Add(separator);

        return string.Join(Environment.NewLine, lines);
    }
}
