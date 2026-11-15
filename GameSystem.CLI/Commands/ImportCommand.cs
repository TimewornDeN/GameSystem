using GameSystem.Core.Abstractions;
using GameSystem.Core.Logging;
using GameSystem.Core.Management;

namespace GameSystem.CLI.Commands;

/// <summary>
/// import &lt;name&gt; | import --list — loads a new character from the profile source (a folder of JSON files)
/// and adds it, with its item and abilities, to the game world.
/// </summary>
public sealed class ImportCommand(
    GameWorld world,
    ICharacterProfileSource profiles,
    IDisplayer displayer,
    ICombatLogger logger)
    : CommandBase("import", "Loads a new character from a profile file: import <name> | import --list")
{
    protected override void Run(CommandContext context)
    {
        if (context.HasOption("list"))
        {
            var names = profiles.List();
            if (names.Count == 0)
                displayer.Info("No character profiles found.");
            else
                displayer.Info("Available profiles: " + string.Join(", ", names));
            return;
        }

        string? name = context.Args.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(name))
        {
            displayer.Error("Usage: import <name>  (use 'import --list' to see the profiles)");
            return;
        }

        try
        {
            var profile = profiles.Load(name);

            if (world.FindCharacter(profile.Name) is not null)
            {
                displayer.Error($"Character '{profile.Name}' already exists.");
                return;
            }

            // Everything is built first; the world changes only when the whole profile was valid.
            var loaded = CharacterProfileLoader.Build(profile, logger);

            if (loaded.Item is not null && world.FindItem(loaded.Item.Name) is null)
                world.Items.Add(loaded.Item);

            foreach (var ability in loaded.Abilities)
            {
                if (world.FindAbility(ability.Id) is null)
                    world.Abilities.Add(ability);
            }

            world.Characters.Add(loaded.Character);
            displayer.Success($"Character '{loaded.Character.Name}' ({loaded.Character.ClassId}) was loaded from '{name}'.");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException or InvalidOperationException)
        {
            displayer.Error($"Could not load '{name}': {ex.Message}");
        }
    }
}
