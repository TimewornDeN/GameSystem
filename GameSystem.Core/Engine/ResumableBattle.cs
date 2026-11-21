namespace GameSystem.Core.Engine;

/// <summary>A battle together with the one fact the session itself does not know: who controls the second fighter.</summary>
public sealed record ResumableBattle(BattleSession Session, bool SecondFighterIsAi);
