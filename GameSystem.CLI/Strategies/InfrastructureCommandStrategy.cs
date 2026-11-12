using GameSystem.CLI.Commands;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Logging;
using GameSystem.Core.Management;

namespace GameSystem.CLI.Strategies;

/// <summary>Category 3 — infrastructure actions: saving, loading and talking to the remote database.</summary>
public sealed class InfrastructureCommandStrategy : CommandStrategyBase
{
    public InfrastructureCommandStrategy(
        GameWorld world,
        IGameStateRepository states,
        IDisplayer displayer,
        ICombatLogger logger,
        IGenshinApiClient? apiClient = null)
        : base("Infrastructure")
    {
        RegisterCommand(new SaveCommand(world, states, displayer));
        RegisterCommand(new LoadCommand(world, states, displayer, logger));
        RegisterCommand(new SavesCommand(states, displayer));
        RegisterCommand(new RemoteCommand(world, displayer, logger, apiClient));
    }
}
