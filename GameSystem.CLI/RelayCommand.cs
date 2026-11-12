using GameSystem.CLI.Interfaces;

namespace GameSystem.CLI;

/// <summary>
/// Universal command implementation wrapping delegates (Open/Closed Principle).
/// </summary>
public class RelayCommand : ICommand
{
    public string Name { get; }
    public string Description { get; }
    private readonly Action<CommandContext> _execute;

    public RelayCommand(string name, string description, Action<CommandContext> execute)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Command name cannot be empty.", nameof(name));

        Name = name;
        Description = description ?? string.Empty;
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    public void Execute(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        _execute(context);
    }
}
