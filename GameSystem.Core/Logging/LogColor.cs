namespace GameSystem.Core.Logging;

/// <summary>
/// Presentation-agnostic color hint for combat log messages.
/// Core has no knowledge of System.ConsoleColor or any other rendering detail.
/// </summary>
public enum LogColor
{
    Default,
    White,
    Gray,
    Red,
    Green,
    Cyan,
    Yellow,
    Magenta,
    DarkYellow,
    DarkRed
}
