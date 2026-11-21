using GameSystem.Core.Abstractions;
using GameSystem.Core.Management;

namespace GameSystem.CLI.Commands;

/// <summary>
/// save &lt;name&gt; — saves the whole game world: catalogs, characters (with their effects and stance), the
/// narrative data of remote characters and, when there is one, the battle in progress.
/// </summary>
public sealed class SaveCommand(GameWorld world, IGameStateRepository repository, IDisplayer displayer)
    : CommandBase("save", "Saves the current game state: save <n>")
{
    protected override void Run(CommandContext context)
    {
        string? name = context.Args.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(name))
        {
            displayer.Error("Usage: save <name>");
            return;
        }

        // Every character produces its own memento; the snapshotter assembles the persistable world state.
        var state = GameStateSnapshotter.Capture(world.Items, world.Abilities, world.Characters, world.Battle, world.RemoteInfo);

        repository.Save(name, state);
        displayer.Success($"Game state saved as '{name}'.");

        if (state.Battle is not null)
            displayer.Info($"The battle in progress (round {state.Battle.Round}) is part of this save: 'continue {name}' resumes it.");
    }
}
