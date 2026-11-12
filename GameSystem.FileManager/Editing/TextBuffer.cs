namespace GameSystem.FileManager.Editing;

/// <summary>
/// Editable text with a cursor. It knows nothing about the screen or the keyboard,
/// which keeps all the editing rules unit-testable.
/// </summary>
public sealed class TextBuffer
{
    private readonly List<string> _lines;
    private int _desiredColumn;

    public TextBuffer(IEnumerable<string> lines)
    {
        _lines = lines.ToList();
        if (_lines.Count == 0)
        {
            _lines.Add(string.Empty);
        }
    }

    public IReadOnlyList<string> Lines => _lines;

    public int Row { get; private set; }

    public int Column { get; private set; }

    /// <summary>True when the text changed since it was loaded or last saved.</summary>
    public bool IsDirty { get; private set; }

    private string CurrentLine => _lines[Row];

    public void Insert(char character)
    {
        _lines[Row] = CurrentLine.Insert(Column, character.ToString());
        Column++;
        Touch();
    }

    public void InsertText(string text)
    {
        foreach (var character in text)
        {
            Insert(character);
        }
    }

    public void InsertNewLine()
    {
        var line = CurrentLine;
        _lines[Row] = line[..Column];
        _lines.Insert(Row + 1, line[Column..]);
        Row++;
        Column = 0;
        Touch();
    }

    public void Backspace()
    {
        if (Column > 0)
        {
            _lines[Row] = CurrentLine.Remove(Column - 1, 1);
            Column--;
        }
        else if (Row > 0)
        {
            var previousLength = _lines[Row - 1].Length;
            _lines[Row - 1] += CurrentLine;
            _lines.RemoveAt(Row);
            Row--;
            Column = previousLength;
        }
        else
        {
            return;
        }

        Touch();
    }

    public void Delete()
    {
        if (Column < CurrentLine.Length)
        {
            _lines[Row] = CurrentLine.Remove(Column, 1);
        }
        else if (Row < _lines.Count - 1)
        {
            _lines[Row] += _lines[Row + 1];
            _lines.RemoveAt(Row + 1);
        }
        else
        {
            return;
        }

        Touch();
    }

    public void MoveLeft()
    {
        if (Column > 0)
        {
            Column--;
        }
        else if (Row > 0)
        {
            Row--;
            Column = CurrentLine.Length;
        }

        _desiredColumn = Column;
    }

    public void MoveRight()
    {
        if (Column < CurrentLine.Length)
        {
            Column++;
        }
        else if (Row < _lines.Count - 1)
        {
            Row++;
            Column = 0;
        }

        _desiredColumn = Column;
    }

    public void MoveUp() => MoveVertically(-1);

    public void MoveDown() => MoveVertically(1);

    /// <summary>Moves by whole lines and tries to keep the column the user was aiming at.</summary>
    public void MoveVertically(int delta)
    {
        Row = Math.Clamp(Row + delta, 0, _lines.Count - 1);
        Column = Math.Min(_desiredColumn, CurrentLine.Length);
    }

    public void MoveHome()
    {
        Column = 0;
        _desiredColumn = Column;
    }

    public void MoveEnd()
    {
        Column = CurrentLine.Length;
        _desiredColumn = Column;
    }

    public void MarkSaved() => IsDirty = false;

    private void Touch()
    {
        _desiredColumn = Column;
        IsDirty = true;
    }
}
