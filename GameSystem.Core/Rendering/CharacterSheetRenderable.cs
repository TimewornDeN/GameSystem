using GameSystem.Core.Characters;
using GameSystem.Core.Management;
using GameSystem.Core.Models;
using GameSystem.Core.Text;

namespace GameSystem.Core.Rendering;

/// <summary>A character sheet. The data is a structured-text document built by the Text module.</summary>
public sealed class CharacterSheetRenderable(Character character, RemoteCharacterInfo? remoteInfo = null)
    : Renderable<IText>(CharacterSheetBuilder.Build(character, remoteInfo));
