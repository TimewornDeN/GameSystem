namespace GameSystem.Core.Text;

/// <summary>Standard text paragraph.</summary>
public class Paragraph(string content) : Leaf
{
    public override string Title { get; } = content ?? string.Empty;

    public override void Accept(ITextVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }
}
