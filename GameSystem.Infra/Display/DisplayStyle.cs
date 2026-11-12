using GameSystem.Core.Abstractions;

namespace GameSystem.Infra.Display;

internal static class DisplayStyle
{
    public static string Prefix(DisplayKind kind) => kind switch
    {
        DisplayKind.Info => "[Info]: ",
        DisplayKind.Success => "[Success]: ",
        DisplayKind.Warning => "[Warning]: ",
        DisplayKind.Error => "[Error]: ",
        _ => string.Empty
    };

    public static ConsoleColor? Color(DisplayKind kind) => kind switch
    {
        DisplayKind.Title => ConsoleColor.Magenta,
        DisplayKind.Info => ConsoleColor.Cyan,
        DisplayKind.Success => ConsoleColor.Green,
        DisplayKind.Warning => ConsoleColor.Yellow,
        DisplayKind.Error => ConsoleColor.Red,
        _ => null
    };
}
