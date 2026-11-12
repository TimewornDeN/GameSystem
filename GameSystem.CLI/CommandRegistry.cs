using GameSystem.CLI.Interfaces;
using GameSystem.Core.Abstractions;

namespace GameSystem.CLI;

/// <summary>
/// Manages command registration, validation, and execution routing.
/// All feedback goes through the injected IDisplayer instead of raw Console calls.
/// </summary>
public class CommandRegistry(ICommandParser parser, IDisplayer displayer) : ICommandRegistry
{
    private readonly Dictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly ICommandParser _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    private readonly IDisplayer _displayer = displayer ?? throw new ArgumentNullException(nameof(displayer));

    public void RegisterCommand(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));

        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Command name cannot be empty.", nameof(command));

        _commands[command.Name] = command;
    }

    public bool ExecuteCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return true;

        var context = _parser.Parse(input);
        if (string.IsNullOrEmpty(context.CommandName)) return true;

        if (_commands.TryGetValue(context.CommandName, out var command))
        {
            try
            {
                command.Execute(context);
            }
            catch (Exception ex)
            {
                _displayer.Error($"Execution Error: {ex.Message}");
            }
            return true;
        }

        _displayer.Warning($"Unknown command '{context.CommandName}'. Type 'help' for the list of commands.");
        return true;
    }

    public IEnumerable<ICommand> GetCommands() => _commands.Values;
}
