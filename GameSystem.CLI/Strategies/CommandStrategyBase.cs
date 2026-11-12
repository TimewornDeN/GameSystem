using GameSystem.CLI.Interfaces;

namespace GameSystem.CLI.Strategies;

/// <summary>Common part of all strategies: a table "command name → command object".</summary>
public abstract class CommandStrategyBase(string name) : ICommandStrategy
{
    private readonly Dictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public string Name { get; } = name;

    public IReadOnlyCollection<string> GetCommandSelectors() => [.. _commands.Keys];

    public IReadOnlyCollection<ICommand> GetCommands() => [.. _commands.Values];

    public void RegisterCommand(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Command name cannot be empty.", nameof(command));

        _commands[command.Name] = command;
    }

    public void ExecuteCommand(string selector, CommandContext args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (!_commands.TryGetValue(selector, out var command))
            throw new KeyNotFoundException($"Strategy '{Name}' has no command '{selector}'.");

        command.Execute(args);
    }
}
