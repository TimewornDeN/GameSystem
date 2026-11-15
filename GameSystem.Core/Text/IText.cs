namespace GameSystem.Core.Text;

/// <summary>Base interface for a node of a structured-text document (reused from the TextSystem module).</summary>
public interface IText
{
    string Title { get; }

    /// <summary>Double dispatch entry point of the Visitor: the node calls the overload matching its own type.</summary>
    void Accept(ITextVisitor visitor);
}
