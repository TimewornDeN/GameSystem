using GameSystem.Core.Abstractions;

namespace GameSystem.CLI.Commands;

/// <summary>list — shows the names of saved games.</summary>
public sealed class SavesCommand(IGameStateRepository repository, IDisplayer displayer)
    : CommandBase("list", "Lists saved games")
{
    protected override void Run(CommandContext context)
    {
        var names = repository.List();
        if (names.Count == 0)
        {
            displayer.Info("No saved games.");
            return;
        }

        displayer.Info("Saved games:");
        foreach (string name in names)
        {
            displayer.Show($"  {name}");
        }
    }
}
