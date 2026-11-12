using GameSystem.CLI.Interfaces;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Rendering;

namespace GameSystem.CLI.Commands;

/// <summary>results — shows the history of finished games.</summary>
public sealed class ResultsCommand(IGameResultRepository results, IDisplayer displayer, IRenderableDisplay display)
    : CommandBase("results", "Shows the history of finished games")
{
    protected override void Run(CommandContext context)
    {
        IReadOnlyList<GameSystem.Core.Models.GameResult> all;
        try
        {
            all = results.GetAll();
        }
        catch (InvalidDataException ex)
        {
            displayer.Error(ex.Message);
            return;
        }

        if (all.Count == 0)
        {
            displayer.Info("No finished games yet. Start one with 'play'.");
            return;
        }

        displayer.Show("=== Game Results ===", DisplayKind.Title);
        display.Display(new GameResultsRenderable(all));
    }
}
