namespace GameSystem.CLI.Interfaces;

public interface ICommandRegistry
{
    void RegisterCommand(ICommand command);
    bool ExecuteCommand(string input);
    IEnumerable<ICommand> GetCommands();
}
