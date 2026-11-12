using GameSystem.CLI.Commands;
using GameSystem.CLI.Interfaces;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Management;

namespace GameSystem.CLI.Strategies;

/// <summary>Category 1 — showing information: lists, character sheets, results, help. Nothing is changed.</summary>
public sealed class InformationCommandStrategy : CommandStrategyBase
{
    public InformationCommandStrategy(
        GameWorld world,
        IDisplayer displayer,
        IRenderableDisplay display,
        IGameResultRepository results,
        ICommandCatalog catalog)
        : base("Information")
    {
        RegisterCommand(new LsCommand(world, displayer, display));
        RegisterCommand(new SheetCommand(world, displayer, display));
        RegisterCommand(new ResultsCommand(results, displayer, display));
        RegisterCommand(new HelpCommand(catalog, displayer));
    }
}
