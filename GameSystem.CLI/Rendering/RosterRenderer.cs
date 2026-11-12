using GameSystem.CLI.Graphics;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Characters;

namespace GameSystem.CLI.Rendering;

/// <summary>Shows characters as a table with a health bar.</summary>
public sealed class RosterRenderer(IDisplayer displayer) : IRenderer<IReadOnlyList<Character>>
{
    private readonly IDisplayer _displayer = displayer ?? throw new ArgumentNullException(nameof(displayer));

    public void Render(IReadOnlyList<Character> characters)
    {
        var table = new ConsoleTable("#", "Name", "Class", "HP", "Health", "Armor", "Attack", "Item");

        for (int i = 0; i < characters.Count; i++)
        {
            var c = characters[i];
            table.AddRow(
                (i + 1).ToString(),
                c.Name,
                c.ClassId,
                $"{c.Health}/{c.MaxHealth}",
                ProgressBar.Render(c.Health, c.MaxHealth),
                c.Armor.ToString(),
                c.AttackPower.ToString(),
                c.EquippedItem?.Name ?? "-");
        }

        _displayer.Show(table.ToString());
    }
}
