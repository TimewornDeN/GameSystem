namespace GameSystem.Presentation.Console;

/// <summary>How a line of the game window is coloured.</summary>
public enum LineStyle
{
    Plain,
    Title,
    Info,
    Success,
    Warning,
    Error,
    Echo,
    Damage,
    Heal,
    Buff,
    Magic,
    Dim,
    Gold
}

/// <summary>One entry of the output field of the window: the text and a style hint for the colour.</summary>
public sealed record ConsoleLine(string Text, LineStyle Style = LineStyle.Plain);
