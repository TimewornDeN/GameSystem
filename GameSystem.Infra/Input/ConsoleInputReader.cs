using GameSystem.Core.Abstractions;

namespace GameSystem.Infra.Input;

/// <summary>Reads user input from the console.</summary>
public class ConsoleInputReader : IInputReader
{
    public string? ReadLine(string? prompt = null)
    {
        if (!string.IsNullOrEmpty(prompt))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(prompt);
            Console.ResetColor();
        }

        return Console.ReadLine();
    }
}
