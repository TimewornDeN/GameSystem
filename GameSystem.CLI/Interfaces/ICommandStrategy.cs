namespace GameSystem.CLI.Interfaces;

/// <summary>
/// STRATEGY. A group of related commands (for example "information" or "infrastructure").
/// The <see cref="Cli"/> facade is parameterized with strategies and sends every command to the one that owns it.
/// </summary>
public interface ICommandStrategy
{
    /// <summary>The category of the commands, shown by the help command.</summary>
    string Name { get; }

    /// <summary>The words that select a command of this strategy (the command names).</summary>
    IReadOnlyCollection<string> GetCommandSelectors();

    IReadOnlyCollection<ICommand> GetCommands();

    void RegisterCommand(ICommand command);

    /// <summary>Runs the command that belongs to <paramref name="selector"/> with already parsed arguments.</summary>
    void ExecuteCommand(string selector, CommandContext args);
}
