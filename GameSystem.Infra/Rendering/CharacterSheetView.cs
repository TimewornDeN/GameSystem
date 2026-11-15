using GameSystem.Core.Abstractions;
using GameSystem.Core.Text;

namespace GameSystem.Infra.Rendering;

/// <summary>Display DTO wrapping a rendered structured-text document (e.g. a character sheet).</summary>
public sealed class CharacterSheetView(IText document) : IDisplayable
{
    public string Text => TextRenderer.Render(document);
    public DisplayKind Kind => DisplayKind.Plain;
}
