using GameSystem.CLI.Battle;
using GameSystem.CLI.Interfaces;
using GameSystem.CLI.Rendering;
using GameSystem.CLI.Strategies;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Engine;
using GameSystem.Core.Logging;
using GameSystem.Core.Management;

namespace GameSystem.CLI;

/// <summary>
/// Assembles the complete command interface: the facade, the three category strategies and the renderers.
/// The real program and the tests use the same assembly, only the injected implementations differ.
/// </summary>
public static class CliFactory
{
    public static Cli Create(
        IDisplayer displayer,
        IInputReader input,
        ICombatLogger logger,
        IGameStateRepository states,
        IGameResultRepository results,
        IGenshinApiClient? apiClient = null,
        GameWorld? world = null,
        ICommandParser? parser = null,
        ITurnStrategy? opponentStrategy = null,
        Func<DateTime>? clock = null)
    {
        world ??= new GameWorld();
        parser ??= new CommandParser();
        opponentStrategy ??= new RandomTurnStrategy();
        clock ??= () => DateTime.UtcNow;

        var cli = new Cli(parser, displayer);

        // How things look is decided here, in the presentation layer.
        cli.UseRenderer(new TextDocumentRenderer(displayer));
        cli.UseRenderer(new RosterRenderer(displayer));
        cli.UseRenderer(new GameResultsRenderer(displayer));

        var battle = new BattleController(parser, displayer, input, opponentStrategy, results, clock);

        cli.UseStrategy(new InformationCommandStrategy(world, displayer, cli, results, cli));
        cli.UseStrategy(new TransformativeCommandStrategy(world, displayer, input, cli, logger, battle));
        cli.UseStrategy(new InfrastructureCommandStrategy(world, states, displayer, logger, apiClient));

        return cli;
    }
}
