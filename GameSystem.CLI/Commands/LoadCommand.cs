using GameSystem.Core.Abstractions;
using GameSystem.Core.Logging;
using GameSystem.Core.Management;

namespace GameSystem.CLI.Commands;

/// <summary>load &lt;name&gt; — replaces the current world with a saved one.</summary>
public sealed class LoadCommand(GameWorld world, IGameStateRepository repository, IDisplayer displayer, ICombatLogger logger)
    : CommandBase("load", "Loads a saved game state: load <n>")
{
    protected override void Run(CommandContext context)
    {
        string? name = context.Args.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(name))
        {
            displayer.Error("Usage: load <name>");
            return;
        }

        if (!SavedGameLoader.TryLoad(name, world, repository, displayer, logger))
            return;

        displayer.Success($"Game state '{name}' loaded.");

        if (world.Battle is { } battle)
        {
            displayer.Info(
                $"This save has a battle in progress ({battle.Session.First.Name} vs {battle.Session.Second.Name}, " +
                $"round {battle.Session.Round}). Type 'continue {name}' to go on with it.");
        }
    }
}
