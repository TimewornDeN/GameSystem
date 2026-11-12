using GameSystem.CLI.Battle;
using GameSystem.CLI.Interfaces;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Characters;
using GameSystem.Core.Engine;
using GameSystem.Core.Management;
using GameSystem.Core.Rendering;

namespace GameSystem.CLI.Commands;

/// <summary>
/// play [&lt;char1&gt; &lt;char2&gt;] [--ai] — chooses the fighters (by names or from a list), starts a battle
/// and runs its main loop. Put --ai at the end: then the second fighter is played by the computer.
/// </summary>
public sealed class PlayCommand(
    GameWorld world,
    IDisplayer displayer,
    IInputReader input,
    IRenderableDisplay display,
    BattleController battle)
    : CommandBase("play", "Starts a battle: play [<char1> <char2>] [--ai]  (without names you choose from a list)")
{
    protected override void Run(CommandContext context)
    {
        var available = world.Characters.Where(c => c.Health > 0).ToList();
        if (available.Count < 2)
        {
            displayer.Error("At least two living characters are required to play. Create them with 'create char' or 'remote create'.");
            return;
        }

        Character? first;
        Character? second;

        if (context.Args.Count >= 2)
        {
            first = Resolve(context.Args[0]);
            second = Resolve(context.Args[1]);
            if (first is null || second is null) return;

            if (ReferenceEquals(first, second))
            {
                displayer.Error("Choose two different characters.");
                return;
            }
        }
        else
        {
            displayer.Show("=== Choose your fighters ===", DisplayKind.Title);
            display.Display(new RosterRenderable(available));

            first = Choose("Choose player 1 (number or name): ", available, exclude: null);
            if (first is null) return;

            second = Choose("Choose player 2 (number or name): ", available, exclude: first);
            if (second is null) return;
        }

        battle.Run(new BattleSession(first, second), context.HasOption("ai"));
    }

    private Character? Resolve(string name)
    {
        var character = world.FindCharacter(name);
        if (character is null)
        {
            displayer.Error($"Character '{name}' not found.");
            return null;
        }

        if (character.Health <= 0)
        {
            displayer.Error($"Character '{name}' is defeated (0 HP) and cannot fight. Load a saved game or choose another one.");
            return null;
        }

        return character;
    }

    private Character? Choose(string prompt, IReadOnlyList<Character> options, Character? exclude)
    {
        while (true)
        {
            string? raw = input.ReadLine(prompt);
            if (raw is null)
            {
                displayer.Info("Operation cancelled.");
                return null;
            }

            raw = raw.Trim();
            Character? chosen = int.TryParse(raw, out int number) && number >= 1 && number <= options.Count
                ? options[number - 1]
                : options.FirstOrDefault(c => c.Name.Equals(raw, StringComparison.OrdinalIgnoreCase));

            if (chosen is null)
            {
                displayer.Warning("Invalid choice. Enter a number from the table or a character name.");
                continue;
            }

            if (ReferenceEquals(chosen, exclude))
            {
                displayer.Warning("This character is already chosen. Pick another one.");
                continue;
            }

            return chosen;
        }
    }
}
