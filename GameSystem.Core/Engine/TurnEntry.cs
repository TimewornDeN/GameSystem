namespace GameSystem.Core.Engine;

/// <summary>One line of the battle history: who did what in which round and how the health changed.</summary>
public sealed record TurnEntry(
    int Round,
    int Sequence,
    string Actor,
    string Opponent,
    string Action,
    int ActorHealthBefore,
    int ActorHealthAfter,
    int OpponentHealthBefore,
    int OpponentHealthAfter)
{
    /// <summary>Health the opponent lost during this action.</summary>
    public int DamageDealt => Math.Max(0, OpponentHealthBefore - OpponentHealthAfter);

    /// <summary>Health the actor regained during this action.</summary>
    public int HealthRestored => Math.Max(0, ActorHealthAfter - ActorHealthBefore);
}
