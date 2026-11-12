using GameSystem.Core.Abstractions;
using GameSystem.Core.Management;

namespace GameSystem.CLI.Commands;

/// <summary>add --char_id &lt;n&gt; --id &lt;item|ability_id&gt; — equips an item or teaches an ability.</summary>
public sealed class AddCommand(GameWorld world, IDisplayer displayer)
    : CommandBase("add", "Equips an item or teaches an ability: add --char_id <n> --id <item|ability_id>")
{
    protected override void Run(CommandContext context)
    {
        string? charId = context.GetOption("char_id");
        string? targetId = context.GetOption("id");

        if (string.IsNullOrWhiteSpace(charId) || string.IsNullOrWhiteSpace(targetId))
        {
            displayer.Error("Usage: add --char_id <n> --id <item|ability_id>");
            return;
        }

        var character = world.FindCharacter(charId);
        if (character is null)
        {
            displayer.Error($"Character '{charId}' not found.");
            return;
        }

        var item = world.FindItem(targetId);
        if (item is not null)
        {
            character.Equip(item);
            displayer.Success($"Equipped item '{item.Name}' on character '{character.Name}'.");
            return;
        }

        var ability = world.FindAbility(targetId);
        if (ability is not null)
        {
            try
            {
                character.Learn(ability);
                displayer.Success($"Taught ability '{ability.Id}' ({ability.Description}) to character '{character.Name}'.");
            }
            catch (InvalidOperationException ex)
            {
                displayer.Error(ex.Message);
            }

            return;
        }

        displayer.Error($"Neither item nor ability with ID/Name '{targetId}' was found.");
    }
}
