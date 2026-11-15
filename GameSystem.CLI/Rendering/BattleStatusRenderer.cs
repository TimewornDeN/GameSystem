using GameSystem.CLI.Graphics;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Characters;
using GameSystem.Core.Engine;

namespace GameSystem.CLI.Rendering;

/// <summary>Shows both fighters with health bars and tells whose turn it is.</summary>
public sealed class BattleStatusRenderer(IDisplayer displayer) : IRenderer<BattleSession>
{
    private readonly IDisplayer _displayer = displayer ?? throw new ArgumentNullException(nameof(displayer));

    public void Render(BattleSession session)
    {
        _displayer.Show($"--- Round {session.Round} ---", DisplayKind.Title);
        ShowFighter(session.First, session);
        ShowFighter(session.Second, session);

        if (!session.IsFinished)
            _displayer.Info($"Turn of {session.CurrentActor.Name}.");
    }

    private void ShowFighter(Character fighter, BattleSession session)
    {
        _displayer.Show(FighterLine(fighter, session));

        if (fighter.ActiveEffects.Count > 0)
            _displayer.Show("    effects: " + string.Join("; ", fighter.ActiveEffects));
    }

    private static string FighterLine(Character fighter, BattleSession session)
    {
        string marker = !session.IsFinished && ReferenceEquals(fighter, session.CurrentActor) ? ">" : " ";
        string defending = fighter.IsDefending ? " (defending)" : string.Empty;
        return $"{marker} {fighter.Name,-14} {ProgressBar.Render(fighter.Health, fighter.MaxHealth, 20)} {fighter.Health}/{fighter.MaxHealth}{defending}";
    }
}
