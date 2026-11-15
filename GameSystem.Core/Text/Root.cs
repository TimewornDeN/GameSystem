namespace GameSystem.Core.Text;

/// <summary>Root container of a structured-text document.</summary>
public class Root(string title) : Container(title)
{
    public override void Accept(ITextVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }
}
