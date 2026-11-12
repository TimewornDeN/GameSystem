using GameSystem.CLI.Interfaces;
using GameSystem.Core.Abstractions;

namespace GameSystem.CLI.Commands;

/// <summary>help — lists the commands of every strategy, grouped by category.</summary>
public sealed class HelpCommand(ICommandCatalog catalog, IDisplayer displayer, string header = "Available commands (plus 'exit'):")
    : CommandBase("help", "Lists available commands")
{
    protected override void Run(CommandContext context)
    {
        displayer.Info(header);

        foreach (var strategy in catalog.Strategies)
        {
            displayer.Show($"[{strategy.Name}]", DisplayKind.Title);
            foreach (var command in strategy.GetCommands())
                displayer.Show($"  {command.Name,-8} {command.Description}");
        }
    }
}
