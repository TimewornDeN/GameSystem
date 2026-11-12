namespace GameSystem.Core.Logging;

/// <summary>Interface for handling formatted combat messages with color support.</summary>
public interface ICombatLogger
{
    /// <summary>Logs a message to the output with a suggested color.</summary>
    /// <param name="message">The text message to log.</param>
    /// <param name="color">A presentation hint for the message's color.</param>
    void Log(string message, LogColor color = LogColor.Default);
}
