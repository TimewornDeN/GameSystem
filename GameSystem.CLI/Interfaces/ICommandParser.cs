namespace GameSystem.CLI.Interfaces;

public interface ICommandParser
{
    CommandContext Parse(string input);
}
