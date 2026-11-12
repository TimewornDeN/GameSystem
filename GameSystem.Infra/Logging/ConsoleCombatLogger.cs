using GameSystem.Core.Logging;

namespace GameSystem.Infra.Logging;

/// <summary>Console implementation of the combat logger; maps Core's presentation-agnostic LogColor to System.ConsoleColor.</summary>
public class ConsoleCombatLogger : ICombatLogger
{
    public void Log(string message, LogColor color = LogColor.Default)
    {
        ConsoleColor? mapped = Map(color);

        if (mapped is null)
        {
            Console.WriteLine(message);
            return;
        }

        Console.ForegroundColor = mapped.Value;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static ConsoleColor? Map(LogColor color) => color switch
    {
        LogColor.White => ConsoleColor.White,
        LogColor.Gray => ConsoleColor.Gray,
        LogColor.Red => ConsoleColor.Red,
        LogColor.Green => ConsoleColor.Green,
        LogColor.Cyan => ConsoleColor.Cyan,
        LogColor.Yellow => ConsoleColor.Yellow,
        LogColor.Magenta => ConsoleColor.Magenta,
        LogColor.DarkYellow => ConsoleColor.DarkYellow,
        LogColor.DarkRed => ConsoleColor.DarkRed,
        _ => null
    };
}
