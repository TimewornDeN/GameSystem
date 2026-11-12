using GameSystem.Core.Characters;
using GameSystem.Core.Logging;

namespace GameSystem.Core.Engine;

/// <summary>
/// Manages combat execution and turn-based battle simulation between two characters.
/// What a character does on its turn is delegated to an <see cref="ITurnStrategy"/>,
/// so battles can be randomized in the real app and fully scripted in tests.
/// </summary>
/// <param name="logger">Logger service instance for combat logging.</param>
/// <param name="turnStrategy">Turn decision algorithm; defaults to <see cref="RandomTurnStrategy"/>.</param>
public class Game(ICombatLogger logger, ITurnStrategy? turnStrategy = null)
{
    private readonly ICombatLogger _logger = logger
        ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");

    private readonly ITurnStrategy _turnStrategy = turnStrategy ?? new RandomTurnStrategy();

    /// <summary>
    /// Starts a turn-based battle loop between two specified characters until one's health reaches zero.
    /// </summary>
    /// <param name="player1">The first combatant.</param>
    /// <param name="player2">The second combatant.</param>
    public void StartSimulation(Character player1, Character player2)
    {
        if (player1 is null || player2 is null)
        {
            _logger.Log("Cannot start simulation. Both combatants must be provided.", LogColor.DarkRed);
            return;
        }

        _logger.Log($"\n=== DYNAMIC BATTLE: {player1.Name} VS {player2.Name} ===", LogColor.Yellow);

        int round = 1;

        // Loop runs until one of the combatants dies
        while (player1.Health > 0 && player2.Health > 0)
        {
            _logger.Log($"\n--- Round {round} ---", LogColor.DarkYellow);

            ExecuteTurn(player1, player2);
            if (player2.Health <= 0) break;

            ExecuteTurn(player2, player1);

            round++;
        }

        Character winner = player1.Health > 0 ? player1 : player2;
        Character loser = player1.Health > 0 ? player2 : player1;

        _logger.Log("\n=================================", LogColor.Green);
        _logger.Log($"BATTLE FINISHED! [{loser.Name}] was defeated.", LogColor.Red);
        _logger.Log($"WINNER: [{winner.Name}] with {winner.Health}/{winner.MaxHealth} HP remaining!", LogColor.Green);
        _logger.Log("=================================", LogColor.Green);
    }

    /// <summary>The strategy picks a command, the engine only executes it.</summary>
    private void ExecuteTurn(Character attacker, Character defender)
        => _turnStrategy.ChooseAction(attacker, defender).Execute();
}
