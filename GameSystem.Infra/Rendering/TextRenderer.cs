using GameSystem.Core.Text;

namespace GameSystem.Infra.Rendering;

/// <summary>Converts a structured-text document (Core.Text) into its plain-text representation.</summary>
public static class TextRenderer
{
    public static string Render(IText element)
    {
        ArgumentNullException.ThrowIfNull(element);

        var visitor = new PlainTextRenderingVisitor();
        element.Accept(visitor);
        return visitor.Result;
    }
}
