using GameSystem.Core.Abstractions;
using GameSystem.Core.Management;

namespace GameSystem.CLI.Commands;

/// <summary>save &lt;name&gt; — saves the whole game world (catalogs and characters).</summary>
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
        var state = GameStateSnapshotter.Capture(world.Items, world.Abilities, world.Characters);

        repository.Save(name, state);
        displayer.Success($"Game state saved as '{name}'.");
    }
}
