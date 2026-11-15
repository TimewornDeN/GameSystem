using GameSystem.Core.Text;
using GameSystem.Infra.Rendering;
using Xunit;

namespace GameSystem.Tests.Infra;

public class TextVisitorTests
{
    /// <summary>A second, independent visitor: proves operations can be added without touching the node classes.</summary>
    private sealed class CountingVisitor : ITextVisitor
    {
        public int Headings { get; private set; }
        public int Paragraphs { get; private set; }
        public int Links { get; private set; }

        public void Visit(Root root) => VisitChildren(root);

        public void Visit(Heading heading)
        {
            Headings++;
            VisitChildren(heading);
        }

        public void Visit(Paragraph paragraph) => Paragraphs++;

        public void Visit(Link link) => Links++;

        private void VisitChildren(Container container)
        {
            foreach (var child in container.Children)
                child.Accept(this);
        }
    }

    private static Root BuildSampleDocument()
    {
        var root = new Root("Doc");
        var heading = new Heading("Intro");
        heading.AddChild(new Paragraph("Hello"));
        heading.AddChild(new Link("Docs", "https://example.com"));
        var nested = new Heading("Details", 1);
        nested.AddChild(new Paragraph("More"));
        heading.AddChild(nested);
        root.AddChild(heading);
        return root;
    }

    [Fact]
    public void CustomVisitor_WalksTheWholeTree()
    {
        var visitor = new CountingVisitor();

        BuildSampleDocument().Accept(visitor);

        Assert.Equal(2, visitor.Headings);
        Assert.Equal(2, visitor.Paragraphs);
        Assert.Equal(1, visitor.Links);
    }

    [Fact]
    public void Render_PrintsTitleHeadingsAndIndentedChildren()
    {
        string text = TextRenderer.Render(BuildSampleDocument());

        Assert.Contains("Title: Doc", text);
        Assert.Contains("Intro", text);
        Assert.Contains("\tHello", text);
        Assert.Contains("\t[Docs](https://example.com)", text);
        Assert.Contains("\t\tMore", text);
    }

    [Fact]
    public void Accept_NullVisitor_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Paragraph("x").Accept(null!));
    }
}
