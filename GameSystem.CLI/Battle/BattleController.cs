using GameSystem.CLI.Interfaces;
using GameSystem.CLI.Rendering;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Combat.Actions;
using GameSystem.Core.Engine;
using GameSystem.Core.Models;
using GameSystem.Core.Rendering;

namespace GameSystem.CLI.Battle;

/// <summary>
/// The main loop of the game: shows the state, reads a command for the current fighter, executes it,
/// repeats until somebody wins, then saves the result.
/// </summary>
public sealed class BattleController(
    ICommandParser parser,
    IDisplayer displayer,
    IInputReader input,
    ITurnStrategy opponentStrategy,
    IGameResultRepository results,
    Func<DateTime> clock)
{
    /// <summary>
    /// Plays the battle. Returns the saved result, or null when the input ended before the battle was over.
    /// When <paramref name="secondFighterIsAi"/> is set, the second fighter's turns are chosen by the turn strategy.
    /// </summary>
    public GameResult? Run(BattleSession session, bool secondFighterIsAi)
    {
        ArgumentNullException.ThrowIfNull(session);

        var cli = new Cli(parser, displayer);
        cli.UseRenderer(new BattleStatusRenderer(displayer));
        cli.UseStrategy(new BattleCommandStrategy(session, displayer, cli, cli));

        displayer.Show($"=== BATTLE: {session.First.Name} VS {session.Second.Name} ===", DisplayKind.Title);
        displayer.Info("Type 'help' to see the battle commands.");
        cli.Display(new BattleStatusRenderable(session));

        while (!session.IsFinished)
        {
            int turnsBefore = session.TurnsPlayed;

            if (secondFighterIsAi && ReferenceEquals(session.CurrentActor, session.Second))
            {
                PlayAiTurn(session);
            }
            else
            {
                string? line = input.ReadLine($"[{session.CurrentActor.Name}] > ");
                if (line is null)
                {
                    displayer.Warning("Input ended. The battle was abandoned and no result was saved.");
                    return null;
                }

                cli.ExecCommand(line);
            }

            if (session.TurnsPlayed != turnsBefore || session.IsFinished)
                cli.Display(new BattleStatusRenderable(session));
        }

        return Finish(session);
    }

    private void PlayAiTurn(BattleSession session)
    {
        var actor = session.CurrentActor;
        displayer.Info($"{actor.Name} (AI) is making a move...");

        try
        {
            session.Perform(opponentStrategy.ChooseAction(actor, session.CurrentOpponent));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            // A failed AI move must not stall the game: fall back to the safest action.
            session.Perform(new DefendAction(actor));
        }
    }

    private GameResult Finish(BattleSession session)
    {
        var result = session.ToResult(clock());

        displayer.Success($"WINNER: [{result.Winner}] with {result.WinnerHealth}/{result.WinnerMaxHealth} HP after {result.Rounds} round(s). [{result.Loser}] was defeated.");

        try
        {
            results.Add(result);
            displayer.Success("The result was saved (see 'results'). Use 'save <name>' to keep the state of your characters.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            displayer.Error($"Could not save the result: {ex.Message}");
        }

        return result;
    }
}
