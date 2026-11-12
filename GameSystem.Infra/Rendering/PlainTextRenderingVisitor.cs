using System.Text;
using GameSystem.Core.Text;

namespace GameSystem.Infra.Rendering;

/// <summary>
/// Concrete visitor that produces the indented plain-text form of a document.
/// It owns the traversal state (output buffer and current depth), which is why it is created per render.
/// </summary>
internal sealed class PlainTextRenderingVisitor : ITextVisitor
{
    private readonly StringBuilder _builder = new();
    private int _depth;

    public string Result => _builder.ToString().TrimEnd();

    public void Visit(Root root)
    {
        _builder.AppendLine($"Title: {root.Title}");
        VisitChildren(root);
    }

    public void Visit(Heading heading)
    {
        _builder.AppendLine($"\n{Indent(heading.Rank)}{heading.Title}");

        int previousDepth = _depth;
        _depth = heading.Rank + 1; // Children sit one level below the heading.
        VisitChildren(heading);
        _depth = previousDepth;
    }

    public void Visit(Paragraph paragraph)
        => _builder.Append($"{Indent(_depth)}{paragraph.Title}\n\n");

    public void Visit(Link link)
        => _builder.Append($"{Indent(_depth)}[{link.Title}]({link.Url})\n\n");

    private void VisitChildren(Container container)
    {
        foreach (var child in container.Children)
            child.Accept(this);
    }

    private static string Indent(int depth) => new('\t', depth);
}
