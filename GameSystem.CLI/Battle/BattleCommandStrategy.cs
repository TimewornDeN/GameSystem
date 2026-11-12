using GameSystem.CLI.Commands;
using GameSystem.CLI.Interfaces;
using GameSystem.CLI.Strategies;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Engine;

namespace GameSystem.CLI.Battle;

/// <summary>
/// The commands that are available during a battle. A battle gets its own <see cref="Cli"/> with only this strategy,
/// so the main loop of the game uses exactly the same parsing and routing as the main CLI.
/// </summary>
public sealed class BattleCommandStrategy : CommandStrategyBase
{
    public BattleCommandStrategy(
        BattleSession session,
        IDisplayer displayer,
        IRenderableDisplay display,
        ICommandCatalog catalog)
        : base("Battle")
    {
        RegisterCommand(new BattleActionCommand("attack", "Hits the opponent", session, displayer));
        RegisterCommand(new BattleActionCommand("heal", "Heals yourself", session, displayer));
        RegisterCommand(new BattleActionCommand("defend", "Doubles your armor for the next hit", session, displayer));
        RegisterCommand(new BattleActionCommand("ability", "Uses an ability on the opponent: ability [id]", session, displayer));
        RegisterCommand(new BattleStatusCommand(session, display));
        RegisterCommand(new SurrenderCommand(session, displayer));
        RegisterCommand(new HelpCommand(catalog, displayer, "Battle commands:"));
    }
}
