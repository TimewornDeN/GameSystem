using GameSystem.CLI.Interfaces;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Management;
using GameSystem.Core.Rendering;

namespace GameSystem.CLI.Commands;

/// <summary>ls &lt;char|item|ability&gt; [--id &lt;id/name&gt;] — lists objects or shows the details of one.</summary>
public sealed class LsCommand(GameWorld world, IDisplayer displayer, IRenderableDisplay display)
    : CommandBase("ls", "Lists objects or details: ls <char|item|ability> [--id <id/name>]")
{
    protected override void Run(CommandContext context)
    {
        string? category = context.Args.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(category))
        {
            displayer.Error("Usage: ls <char|item|ability> [--id <id>]");
            return;
        }

        string? filterId = context.GetOption("id");

        switch (category.ToLowerInvariant())
        {
            case "char":
                ListCharacters(filterId);
                break;
            case "item":
                ListItems(filterId);
                break;
            case "ability":
                ListAbilities(filterId);
                break;
            default:
                displayer.Error($"Unknown category '{category}'. Allowed: char, item, ability.");
                break;
        }
    }

    private void ListCharacters(string? filterId)
    {
        displayer.Show("=== Characters ===", DisplayKind.Title);

        var query = string.IsNullOrWhiteSpace(filterId)
            ? world.Characters
            : [.. world.Characters.Where(c => c.Name.Equals(filterId, StringComparison.OrdinalIgnoreCase))];

        if (query.Count == 0)
        {
            displayer.Warning("(No characters found)");
            return;
        }

        // Without a filter the roster is drawn as a table (renderer decides how); one character gets a detail line.
        if (string.IsNullOrWhiteSpace(filterId))
        {
            display.Display(new RosterRenderable(query));
            return;
        }

        foreach (var c in query)
        {
            string item = c.EquippedItem?.Name ?? "none";
            string abilities = string.Join(", ", c.Abilities.Select(a => a.Id));
            displayer.Show($"- [{c.Name}] ({c.ClassId}) HP: {c.Health}/{c.MaxHealth} | Armor: {c.Armor} | Attack: {c.AttackPower} | Item: {item} | Abilities: {abilities}");
        }
    }

    private void ListItems(string? filterId)
    {
        displayer.Show("=== Items ===", DisplayKind.Title);

        var query = string.IsNullOrWhiteSpace(filterId)
            ? world.Items
            : [.. world.Items.Where(i => i.Name.Equals(filterId, StringComparison.OrdinalIgnoreCase))];

        if (query.Count == 0)
        {
            displayer.Warning("(No items found)");
            return;
        }

        foreach (var item in query)
        {
            displayer.Show($"- [{item.Name}] +HP {item.HealthBonus} / +Armor {item.ArmorBonus} / +Attack {item.AttackBonus}");
        }
    }

    private void ListAbilities(string? filterId)
    {
        displayer.Show("=== Abilities ===", DisplayKind.Title);

        var query = string.IsNullOrWhiteSpace(filterId)
            ? world.Abilities
            : [.. world.Abilities.Where(a => a.Id.Equals(filterId, StringComparison.OrdinalIgnoreCase))];

        if (query.Count == 0)
        {
            displayer.Warning("(No abilities found)");
            return;
        }

        foreach (var ability in query)
        {
            displayer.Show($"- [{ability.Id}]: {ability.Description}");
        }
    }
}
