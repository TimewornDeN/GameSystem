namespace GameSystem.CLI.Interfaces;

public interface ICommand
{
    string Name { get; }
    string Description { get; }
    void Execute(CommandContext context);
}
