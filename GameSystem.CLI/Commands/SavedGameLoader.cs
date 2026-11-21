using GameSystem.Core.Abstractions;
using GameSystem.Core.Logging;
using GameSystem.Core.Management;
using GameSystem.Core.Models;

namespace GameSystem.CLI.Commands;

/// <summary>
/// The part of loading that <c>load</c> and <c>continue</c> have in common: read the save, rebuild the whole world
/// and only then replace the current one, so a broken save can never leave the session half-replaced.
/// </summary>
internal static class SavedGameLoader
{
    public static bool TryLoad(
        string name,
        GameWorld world,
        IGameStateRepository repository,
        IDisplayer displayer,
        ICombatLogger logger)
    {
        if (!repository.Exists(name))
        {
            displayer.Error($"Saved game '{name}' not found.");
            return false;
        }

        try
        {
            var state = repository.Load(name);
            var restored = GameStateSnapshotter.Restore(state, logger);
            world.ReplaceWith(restored);
            return true;
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentException)
        {
            displayer.Error(ex is InvalidDataException ? ex.Message : $"Saved game '{name}' is corrupted: {ex.Message}");
            return false;
        }
    }
}
