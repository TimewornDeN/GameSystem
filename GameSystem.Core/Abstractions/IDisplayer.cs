using GameSystem.Core.Models;

namespace GameSystem.Core.Abstractions;

/// <summary>Defines how displayable objects are presented (console, file, test buffer...).</summary>
public interface IDisplayer
{
    void Display(IDisplayable item);
}

public static class DisplayerExtensions
{
    public static void Show(this IDisplayer displayer, string text, DisplayKind kind = DisplayKind.Plain)
        => displayer.Display(new Message(text, kind));

    public static void Info(this IDisplayer displayer, string text) => displayer.Show(text, DisplayKind.Info);
    public static void Success(this IDisplayer displayer, string text) => displayer.Show(text, DisplayKind.Success);
    public static void Warning(this IDisplayer displayer, string text) => displayer.Show(text, DisplayKind.Warning);
    public static void Error(this IDisplayer displayer, string text) => displayer.Show(text, DisplayKind.Error);
}
