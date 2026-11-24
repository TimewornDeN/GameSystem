using GameSystem.Presentation.Catalog;

namespace GameSystem.Presentation;

/// <summary>What the game is waiting for right now. The window enables its buttons according to this.</summary>
public enum PromptMode
{
    /// <summary>The game is busy and does not read input.</summary>
    Busy,

    /// <summary>The main prompt: any command of the main interface can be typed.</summary>
    Main,

    /// <summary>A battle is going on and the current fighter must choose an action.</summary>
    Battle,

    /// <summary>A command asks its own question (a name, a number): the next line is an answer, not a command.</summary>
    Dialog
}

/// <summary>
/// An immutable picture of the game, built on the game thread at the moment the game starts waiting for input,
/// when nothing changes the world. The window only ever reads snapshots, never the live game objects.
/// </summary>
public sealed record GameSnapshot(
    IReadOnlyList<CharacterCard> Cards,
    IReadOnlyList<string> Saves,
    string? LatestSave,
    PromptMode Mode,
    string Prompt,
    string BattleSummary,
    IReadOnlyList<ServerCharacterCard>? ServerCatalog = null);
