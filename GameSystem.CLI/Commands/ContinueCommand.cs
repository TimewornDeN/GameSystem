using GameSystem.CLI.Battle;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Logging;
using GameSystem.Core.Management;

namespace GameSystem.CLI.Commands;

/// <summary>
/// continue [&lt;name&gt;] — goes on from a save: loads the last one (or the named one) and, when it holds a battle
/// that was not finished, puts the player back into it exactly where it stopped.
/// </summary>
public sealed class ContinueCommand(
    GameWorld world,
    IGameStateRepository repository,
    IDisplayer displayer,
    ICombatLogger logger,
    BattleController battle)
    : CommandBase("continue", "Continues from the last save (or a named one), including a battle in progress: continue [<n>]")
{
    protected override void Run(CommandContext context)
    {
        string? name = context.Args.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(name))
        {
            name = repository.Latest();
            if (name is null)
            {
                displayer.Error("There are no saved games to continue from. Use 'save <name>' first.");
                return;
            }

            displayer.Info($"Continuing from the last save: '{name}'.");
        }

        if (!SavedGameLoader.TryLoad(name, world, repository, displayer, logger))
            return;

        displayer.Success($"Game state '{name}' loaded.");

        if (world.Battle is not { } saved)
        {
            displayer.Info("This save has no battle in progress. Use 'play' to start a new one.");
            return;
        }

        displayer.Info($"Resuming the battle at round {saved.Session.Round}.");
        battle.Run(saved.Session, saved.SecondFighterIsAi);
    }
}
