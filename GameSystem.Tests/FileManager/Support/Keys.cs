namespace GameSystem.Tests.FileManager.Support;

internal static class Keys
{
    public static ConsoleKeyInfo Press(ConsoleKey key, char keyChar = '\0', bool ctrl = false) =>
        new(keyChar, key, false, false, ctrl);

    /// <summary>A printable character that has no special meaning as a command key.</summary>
    public static ConsoleKeyInfo Text(char character) =>
        new(character, ConsoleKey.NoName, false, false, false);

    public static ConsoleKeyInfo Up => Press(ConsoleKey.UpArrow);

    public static ConsoleKeyInfo Down => Press(ConsoleKey.DownArrow);

    public static ConsoleKeyInfo Right => Press(ConsoleKey.RightArrow);

    public static ConsoleKeyInfo Enter => Press(ConsoleKey.Enter, '\r');

    public static ConsoleKeyInfo Escape => Press(ConsoleKey.Escape, '\u001b');

    public static ConsoleKeyInfo Backspace => Press(ConsoleKey.Backspace, '\b');

    public static ConsoleKeyInfo PageDown => Press(ConsoleKey.PageDown);

    public static ConsoleKeyInfo End => Press(ConsoleKey.End);

    public static ConsoleKeyInfo Q => Press(ConsoleKey.Q, 'q');

    public static ConsoleKeyInfo E => Press(ConsoleKey.E, 'e');

    public static ConsoleKeyInfo CtrlS => Press(ConsoleKey.S, '\u0013', ctrl: true);
}
