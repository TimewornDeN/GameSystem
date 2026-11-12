using GameSystem.CLI.Commands;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Engine;

namespace GameSystem.CLI.Battle;

/// <summary>surrender — the current fighter gives up and the opponent wins.</summary>
public sealed class SurrenderCommand(BattleSession session, IDisplayer displayer)
    : CommandBase("surrender", "The current fighter gives up and loses the battle")
{
    protected override void Run(CommandContext context)
    {
        string name = session.CurrentActor.Name;
        session.Surrender();
        displayer.Warning($"{name} surrenders.");
    }
}
