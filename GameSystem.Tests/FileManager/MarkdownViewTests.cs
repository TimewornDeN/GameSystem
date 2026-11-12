using GameSystem.Core.Text;
using GameSystem.FileManager.Rendering;
using GameSystem.Tests.FileManager.Support;
using Xunit;

namespace GameSystem.Tests.FileManager;

/// <summary>The formatted view reuses the Text module (document tree + Visitor) from the course project.</summary>
public class MarkdownViewTests
{
    private static string Plain(StyledLine line) => string.Concat(line.Spans.Select(span => span.Text));

    private static IReadOnlyList<StyledLine> Render(int width, params string[] lines)
    {
        var visitor = new StyledTextVisitor(width);
        MarkdownDocumentParser.Parse("doc.md", lines).Accept(visitor);
        return visitor.Lines;
    }

    [Fact]
    public void Parser_BuildsNestedHeadingsParagraphsAndLinks()
    {
        var root = MarkdownDocumentParser.Parse(
            "doc.md",
            ["# Title", "intro", "continued", "", "## Sub", "[Docs](https://example.com)", "", "# Second"]);

        Assert.Equal(2, root.Children.Count);

        var title = Assert.IsType<Heading>(root.Children[0]);
        Assert.Equal("Title", title.Title);
        Assert.Equal(0, title.Rank);
        Assert.Equal("intro continued", Assert.IsType<Paragraph>(title.Children[0]).Title);

        var sub = Assert.IsType<Heading>(title.Children[1]);
        Assert.Equal(1, sub.Rank);
        Assert.Equal("https://example.com", Assert.IsType<Link>(sub.Children[0]).Url);

        Assert.Equal("Second", Assert.IsType<Heading>(root.Children[1]).Title);
    }

    [Fact]
    public void Parser_HeadingWithoutText_StaysAParagraph()
    {
        var root = MarkdownDocumentParser.Parse("doc.md", ["#", "text"]);

        Assert.Equal("# text", Assert.IsType<Paragraph>(Assert.Single(root.Children)).Title);
    }

    [Fact]
    public void Parser_ShallowHeadingAfterDeepOne_ClosesTheDeepOne()
    {
        var root = MarkdownDocumentParser.Parse("doc.md", ["## Deep", "# Top"]);

        Assert.Equal(2, root.Children.Count);
    }

    [Fact]
    public void Visitor_IndentsDeeperHeadingsMore()
    {
        var lines = Render(40, "# One", "## Two").Select(Plain).ToList();

        var one = lines.Single(line => line.Contains("▌ One"));
        var two = lines.Single(line => line.Contains("▌ Two"));
        Assert.True(two.IndexOf('▌') > one.IndexOf('▌'));
    }

    [Fact]
    public void Visitor_WrapsLongParagraphsToTheWidth()
    {
        var paragraph = string.Join(' ', Enumerable.Repeat("lorem", 20));

        var lines = Render(30, paragraph);

        Assert.All(lines, line => Assert.True(line.Length <= 30));
        Assert.True(lines.Count(line => Plain(line).Contains("lorem")) > 1);
    }

    [Fact]
    public void Visitor_CutsWordsLongerThanTheRow()
    {
        var lines = Render(20, new string('x', 50));

        Assert.All(lines, line => Assert.True(line.Length <= 20));
    }

    [Fact]
    public void Visitor_NeverEmitsEscapeCharacters()
    {
        var lines = Render(40, "# Head\u001b[1m", "a\u001b[31mb", "[T\u001b](http://x\u001b)");

        Assert.All(lines.SelectMany(line => line.Spans), span => Assert.DoesNotContain('\u001b', span.Text));
    }

    [Fact]
    public void Visitor_RendersLinksWithTitleAndUrl()
    {
        var lines = Render(60, "[Docs](https://example.com)");

        var link = lines.Single(line => Plain(line).Contains("Docs"));
        Assert.Contains("https://example.com", Plain(link));
        Assert.Contains(link.Spans, span => span.Text == "Docs" && span.Style == Palette.LinkTitle);
    }

    [Fact]
    public void Visitor_NeverProducesTwoBlankLinesInARow()
    {
        var lines = Render(40, "# A", "text", "", "## B", "more", "[L](http://x)").Select(Plain).ToList();

        for (var i = 1; i < lines.Count; i++)
        {
            Assert.False(lines[i].Length == 0 && lines[i - 1].Length == 0);
        }
    }

    [Fact]
    public void MarkdownFile_OpensFormatted_AndMTogglesTheRawText()
    {
        var harness = new AppHarness();
        harness.Send(Keys.Down, Keys.Down, Keys.Down, Keys.Enter); // readme.md

        var formatted = harness.Screen;
        Assert.Contains("▌ Title", formatted);
        Assert.Contains("formatted", formatted);
        Assert.DoesNotContain("# Title", formatted);

        harness.Send(Keys.Press(ConsoleKey.M, 'm'));

        var raw = harness.Screen;
        Assert.Contains("  1 │ # Title", raw);
        Assert.DoesNotContain("▌ Title", raw);
    }

    [Fact]
    public void PlainTextFile_HasNoFormattedView()
    {
        var harness = new AppHarness();
        harness.OpenNotes();

        harness.Send(Keys.Press(ConsoleKey.M, 'm'));

        Assert.Contains("available for .md files only", harness.Screen);
        Assert.Contains("  1 │ alpha", harness.Screen);
    }
}
