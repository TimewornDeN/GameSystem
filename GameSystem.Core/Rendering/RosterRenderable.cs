using GameSystem.Core.Characters;

namespace GameSystem.Core.Rendering;

/// <summary>A list of characters (the roster).</summary>
public sealed class RosterRenderable(IReadOnlyList<Character> characters)
    : Renderable<IReadOnlyList<Character>>(characters);
