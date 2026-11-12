using GameSystem.CLI.Graphics;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Models;

namespace GameSystem.CLI.Rendering;

/// <summary>Shows the history of finished games as a table.</summary>
public sealed class GameResultsRenderer(IDisplayer displayer) : IRenderer<IReadOnlyList<GameResult>>
{
    private readonly IDisplayer _displayer = displayer ?? throw new ArgumentNullException(nameof(displayer));

    public void Render(IReadOnlyList<GameResult> results)
    {
        var table = new ConsoleTable("#", "Winner", "Loser", "Rounds", "Winner HP", "Finished (UTC)");

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            table.AddRow(
                (i + 1).ToString(),
                r.Winner,
                r.Loser,
                r.Rounds.ToString(),
                $"{r.WinnerHealth}/{r.WinnerMaxHealth}",
                r.FinishedAtUtc.ToString("yyyy-MM-dd HH:mm"));
        }

        _displayer.Show(table.ToString());
    }
}
