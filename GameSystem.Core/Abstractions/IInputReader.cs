namespace GameSystem.Core.Abstractions;

/// <summary>Source of user input (console, script, test stub...).</summary>
public interface IInputReader
{
    /// <summary>Shows an optional prompt and reads one line. Returns null when input is exhausted.</summary>
    string? ReadLine(string? prompt = null);
}
