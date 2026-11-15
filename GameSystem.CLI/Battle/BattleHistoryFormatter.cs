using GameSystem.CLI.Graphics;
using GameSystem.Core.Engine;

namespace GameSystem.CLI.Battle;

/// <summary>Turns the history of a battle into a table. Used by the <c>history</c> command and at the end of a battle.</summary>
public static class BattleHistoryFormatter
{
    public static string ToTable(IReadOnlyList<TurnEntry> history)
    {
        ArgumentNullException.ThrowIfNull(history);

        var table = new ConsoleTable("#", "Round", "Actor", "Action", "Result");

        foreach (var entry in history)
        {
            table.AddRow(
                entry.Sequence.ToString(),
                entry.Round.ToString(),
                entry.Actor,
                entry.Action,
                Describe(entry));
        }

        return table.ToString();
    }

    private static string Describe(TurnEntry entry)
    {
        if (entry.DamageDealt > 0)
            return $"{entry.Opponent} -{entry.DamageDealt} HP ({entry.OpponentHealthBefore} -> {entry.OpponentHealthAfter})";

        if (entry.HealthRestored > 0)
            return $"{entry.Actor} +{entry.HealthRestored} HP ({entry.ActorHealthBefore} -> {entry.ActorHealthAfter})";

        return "no change in health";
    }
}
