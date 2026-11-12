using GameSystem.CLI.Commands;
using GameSystem.CLI.Interfaces;
using GameSystem.Core.Engine;
using GameSystem.Core.Rendering;

namespace GameSystem.CLI.Battle;

/// <summary>status — shows the health of both fighters and whose turn it is.</summary>
public sealed class BattleStatusCommand(BattleSession session, IRenderableDisplay display)
    : CommandBase("status", "Shows both fighters and the current round")
{
    protected override void Run(CommandContext context) => display.Display(new BattleStatusRenderable(session));
}
