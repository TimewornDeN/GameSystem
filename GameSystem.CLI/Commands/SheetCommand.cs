using GameSystem.CLI.Interfaces;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Management;
using GameSystem.Core.Rendering;

namespace GameSystem.CLI.Commands;

/// <summary>sheet &lt;char_name&gt; — shows the character as a structured-text document.</summary>
public sealed class SheetCommand(GameWorld world, IDisplayer displayer, IRenderableDisplay display)
    : CommandBase("sheet", "Renders a character's structured-text sheet: sheet <char_name>")
{
    protected override void Run(CommandContext context)
    {
        string? charName = context.Args.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(charName))
        {
            displayer.Error("Usage: sheet <char_name>");
            return;
        }

        var character = world.FindCharacter(charName);
        if (character is null)
        {
            displayer.Error($"Character '{charName}' not found.");
            return;
        }

        world.RemoteInfo.TryGetValue(character.Name, out var info);
        display.Display(new CharacterSheetRenderable(character, info));
    }
}
