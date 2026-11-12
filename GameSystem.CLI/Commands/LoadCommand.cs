using GameSystem.Core.Abstractions;
using GameSystem.Core.Logging;
using GameSystem.Core.Management;
using GameSystem.Core.Models;

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

        if (!repository.Exists(name))
        {
            displayer.Error($"Saved game '{name}' not found.");
            return;
        }

        GameStateModel state;
        try
        {
            state = repository.Load(name);
        }
        catch (InvalidDataException ex)
        {
            displayer.Error(ex.Message);
            return;
        }

        // Build the whole restored world first, so a failure cannot leave the session half-replaced.
        var restored = GameStateSnapshotter.Restore(state, logger);
        world.ReplaceWith(restored);

        displayer.Success($"Game state '{name}' loaded.");
    }
}
