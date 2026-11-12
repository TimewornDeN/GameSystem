using GameSystem.Core.Abstractions;

namespace GameSystem.CLI.Commands;

/// <summary>Small helper for dialogs: asks a question and validates the answer.</summary>
public sealed class Prompter(IInputReader input, IDisplayer displayer)
{
    private readonly IInputReader _input = input ?? throw new ArgumentNullException(nameof(input));
    private readonly IDisplayer _displayer = displayer ?? throw new ArgumentNullException(nameof(displayer));

    /// <summary>Asks for a line of text. Returns an empty string when the input is over.</summary>
    public string Ask(string prompt) => _input.ReadLine(prompt)?.Trim() ?? string.Empty;

    /// <summary>Asks until the user enters a non-negative integer. Returns false when the input is over.</summary>
    public bool TryReadNonNegativeInt(string prompt, out int value)
    {
        while (true)
        {
            string? raw = _input.ReadLine(prompt);
            if (raw is null)
            {
                value = 0;
                return false;
            }

            if (int.TryParse(raw, out value) && value >= 0)
                return true;

            _displayer.Warning("Invalid number. Please enter a non-negative integer.");
        }
    }
}
