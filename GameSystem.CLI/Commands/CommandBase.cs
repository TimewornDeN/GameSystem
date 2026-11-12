using GameSystem.CLI.Interfaces;

namespace GameSystem.CLI.Commands;

/// <summary>COMMAND. One command is one class with its own dependencies instead of a lambda in the composition root.</summary>
public abstract class CommandBase(string name, string description) : ICommand
{
    public string Name { get; } = name;

    public string Description { get; } = description;

    public void Execute(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Run(context);
    }

    protected abstract void Run(CommandContext context);
}
