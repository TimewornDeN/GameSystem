namespace GameSystem.Core.Text;

/// <summary>Base class for leaf nodes (elements without children): paragraphs, links.</summary>
public abstract class Leaf : IText
{
    public abstract string Title { get; }

    public abstract void Accept(ITextVisitor visitor);
}
