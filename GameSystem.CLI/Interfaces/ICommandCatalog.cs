namespace GameSystem.CLI.Interfaces;

/// <summary>Read-only view of the strategies (and so of all commands) known to a CLI. Used by the help command.</summary>
public interface ICommandCatalog
{
    IReadOnlyList<ICommandStrategy> Strategies { get; }
}
