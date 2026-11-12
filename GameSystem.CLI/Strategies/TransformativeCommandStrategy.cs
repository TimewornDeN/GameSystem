using GameSystem.CLI.Battle;
using GameSystem.CLI.Commands;
using GameSystem.CLI.Interfaces;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Logging;
using GameSystem.Core.Management;

namespace GameSystem.CLI.Strategies;

/// <summary>Category 2 — transformative actions: they change the game world (create, equip, act, play a battle).</summary>
public sealed class TransformativeCommandStrategy : CommandStrategyBase
{
    public TransformativeCommandStrategy(
        GameWorld world,
        IDisplayer displayer,
        IInputReader input,
        IRenderableDisplay display,
        ICombatLogger logger,
        BattleController battle)
        : base("Transformative")
    {
        RegisterCommand(new CreateCommand(world, displayer, new Prompter(input, displayer), logger));
        RegisterCommand(new AddCommand(world, displayer));
        RegisterCommand(new ActCommand(world, displayer));
        RegisterCommand(new PlayCommand(world, displayer, input, display, battle));
    }
}
