namespace GameSystem.Core.Text;

/// <summary>Heading element; its rank defines the nesting/indent level.</summary>
public class Heading(string name, int rank = 0) : Container(name)
{
    public int Rank { get; } = rank >= 0 ? rank : throw new ArgumentOutOfRangeException(nameof(rank), "Heading rank cannot be negative.");

    public override void Accept(ITextVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }
}
