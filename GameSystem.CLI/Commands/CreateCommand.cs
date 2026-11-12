using GameSystem.Core.Abstractions;
using GameSystem.Core.Combat;
using GameSystem.Core.Logging;
using GameSystem.Core.Management;
using GameSystem.Core.Models;

namespace GameSystem.CLI.Commands;

/// <summary>create &lt;char|item|ability&gt; — interactive dialog that creates a character, an item or an ability.</summary>
public sealed class CreateCommand(GameWorld world, IDisplayer displayer, Prompter prompter, ICombatLogger logger)
    : CommandBase("create", "Creates a char, item, or ability: create <char|item|ability>")
{
    protected override void Run(CommandContext context)
    {
        string? type = context.Args.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(type))
        {
            displayer.Error("Usage: create <char|item|ability>");
            return;
        }

        switch (type.ToLowerInvariant())
        {
            case "char":
                CreateCharacter();
                break;
            case "item":
                CreateItem();
                break;
            case "ability":
                CreateAbility();
                break;
            default:
                displayer.Error($"Unknown type '{type}'. Allowed: char, item, ability.");
                break;
        }
    }

    private void CreateCharacter()
    {
        string name = prompter.Ask("Enter character name: ");
        if (string.IsNullOrWhiteSpace(name))
        {
            displayer.Error("Character name cannot be empty.");
            return;
        }

        if (world.FindCharacter(name) is not null)
        {
            displayer.Error($"A character named '{name}' already exists.");
            return;
        }

        while (true)
        {
            string cls = prompter.Ask("Choose class (1 - Warrior, 2 - Mage): ");
            string? classId = cls switch { "1" => "warrior", "2" => "mage", _ => null };

            if (classId is null)
            {
                displayer.Warning("Invalid choice! Please enter '1' for Warrior or '2' for Mage.");
                continue;
            }

            var character = CharacterFactory.Create(classId, name, logger);
            world.Characters.Add(character);
            displayer.Success($"Created {character.ClassId} '{name}'.");
            break;
        }
    }

    private void CreateItem()
    {
        string name = prompter.Ask("Enter item name: ");
        if (string.IsNullOrWhiteSpace(name))
        {
            displayer.Error("Item name cannot be empty.");
            return;
        }

        if (world.FindItem(name) is not null)
        {
            displayer.Error($"Item '{name}' already exists.");
            return;
        }

        world.Items.Add(new Item(name));
        displayer.Success($"Created item '{name}'.");
    }

    private void CreateAbility()
    {
        string id = prompter.Ask("Enter ability ID or Name: ");
        if (string.IsNullOrWhiteSpace(id))
        {
            displayer.Error("Ability ID cannot be empty.");
            return;
        }

        if (world.FindAbility(id) is not null)
        {
            displayer.Error($"Ability with ID/Name '{id}' already exists.");
            return;
        }

        string desc = prompter.Ask("Enter ability description: ");
        if (!prompter.TryReadNonNegativeInt("Enter ability power: ", out int power))
        {
            displayer.Info("Operation cancelled.");
            return;
        }

        var ability = new Ability(new AbilityModel(id, string.IsNullOrWhiteSpace(desc) ? id : desc, power));
        world.Abilities.Add(ability);
        displayer.Success($"Created ability '{id}' ({ability.Description}, power {power}).");
    }
}
