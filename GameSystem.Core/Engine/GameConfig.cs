using GameSystem.Core.Characters;

namespace GameSystem.Core.Engine;

/// <summary>
/// Everything a game needs to start: who fights. It is a plain value that the presentation layer or the
/// persistence layer can prepare and hand to <see cref="BattleSession"/> (the "Game" of the class diagram).
/// </summary>
public sealed class GameConfig
{
    public GameConfig(IReadOnlyList<Character> fighters)
    {
        ArgumentNullException.ThrowIfNull(fighters);

        if (fighters.Count != 2)
            throw new ArgumentException("A battle needs exactly two fighters.", nameof(fighters));

        if (fighters.Any(f => f is null))
            throw new ArgumentException("Fighters cannot be null.", nameof(fighters));

        Fighters = [.. fighters];
    }

    public IReadOnlyList<Character> Fighters { get; }
}
