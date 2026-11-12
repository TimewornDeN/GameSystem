namespace GameSystem.Core.Text;

/// <summary>Hyperlink element.</summary>
public class Link : Leaf
{
    public string Url { get; }
    public override string Title { get; }

    public Link(string title, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));

        Title = string.IsNullOrWhiteSpace(title) ? url : title;
        Url = url;
    }

    public override void Accept(ITextVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }
}
