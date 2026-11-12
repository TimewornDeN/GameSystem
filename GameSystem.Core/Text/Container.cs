namespace GameSystem.Core.Text;

/// <summary>Base class for container nodes that can hold child elements.</summary>
public abstract class Container : IText
{
    private readonly List<IText> _children = [];

    public string Title { get; }
    public IReadOnlyList<IText> Children => _children;

    protected Container(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Container title cannot be empty.", nameof(title));

        Title = title;
    }

    public void AddChild(IText child)
    {
        ArgumentNullException.ThrowIfNull(child);
        _children.Add(child);
    }

    public abstract void Accept(ITextVisitor visitor);
}
