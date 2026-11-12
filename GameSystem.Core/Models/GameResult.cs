namespace GameSystem.Core.Models;

/// <summary>The outcome of one finished battle.</summary>
public sealed record GameResult(
    string Winner,
    string Loser,
    int Rounds,
    int WinnerHealth,
    int WinnerMaxHealth,
    DateTime FinishedAtUtc);
