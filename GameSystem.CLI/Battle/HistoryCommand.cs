using GameSystem.CLI.Commands;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Engine;

namespace GameSystem.CLI.Battle;

/// <summary>history — shows every action of the current battle, one row per action.</summary>
public sealed class HistoryCommand(BattleSession session, IDisplayer displayer)
    : CommandBase("history", "Shows the history of the battle: who did what and how the health changed")
{
    protected override void Run(CommandContext context)
    {
        var history = session.History;
        if (history.Count == 0)
        {
            displayer.Info("Nothing has happened yet.");
            return;
        }

        displayer.Show(BattleHistoryFormatter.ToTable(history));
    }
}
