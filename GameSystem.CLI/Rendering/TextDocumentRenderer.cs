using GameSystem.Core.Abstractions;
using GameSystem.Core.Text;
using GameSystem.Infra.Rendering;

namespace GameSystem.CLI.Rendering;

/// <summary>Shows a structured-text document (character sheet) as plain text, using the visitor of the Text module.</summary>
public sealed class TextDocumentRenderer(IDisplayer displayer) : IRenderer<IText>
{
    private readonly IDisplayer _displayer = displayer ?? throw new ArgumentNullException(nameof(displayer));

    public void Render(IText document) => _displayer.Show(TextRenderer.Render(document));
}
