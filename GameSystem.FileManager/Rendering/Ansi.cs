namespace GameSystem.FileManager.Rendering;

/// <summary>ANSI escape sequences used by the application (see the gist linked in the assignment).</summary>
public static class Ansi
{
    public const string Reset = "\u001b[0m";
    public const string HideCursor = "\u001b[?25l";
    public const string ShowCursor = "\u001b[?25h";
    public const string EnterAlternateScreen = "\u001b[?1049h";
    public const string ExitAlternateScreen = "\u001b[?1049l";
    public const string ClearScreen = "\u001b[2J";

    /// <summary>Moves the cursor to a 1-based row and column.</summary>
    public static string MoveTo(int row, int column) => $"\u001b[{row};{column}H";
}
