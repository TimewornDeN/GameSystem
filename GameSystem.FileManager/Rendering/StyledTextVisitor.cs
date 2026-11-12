using System.Text;
using GameSystem.Core.Text;

namespace GameSystem.FileManager.Rendering;

/// <summary>
/// A second concrete <see cref="ITextVisitor"/> next to the plain-text renderer of GameSystem.Infra:
/// it walks the same document tree but produces colored, word-wrapped canvas rows.
/// Adding it required no change to the node classes (the point of the Visitor pattern).
/// </summary>
public sealed class StyledTextVisitor(int width) : ITextVisitor
{
    private static readonly Style[] HeadingStyles =
    [
        new(Foreground: 213, Bold: true),
        new(Foreground: 81, Bold: true),
        new(Foreground: 114, Bold: true),
        new(Foreground: 221, Bold: true),
    ];

    private readonly List<StyledLine> _lines = [];
    private readonly int _width = Math.Max(10, width);
    private int _depth;

    public IReadOnlyList<StyledLine> Lines => _lines;

    public void Visit(Root root)
    {
        _lines.Add(new StyledLine().Add($" {TextSanitizer.Expand(root.Title)} ", Palette.DocumentTitle));
        VisitChildren(root);
    }

    public void Visit(Heading heading)
    {
        AddBlankLine();
        var style = HeadingStyles[Math.Min(heading.Rank, HeadingStyles.Length - 1)];
        _lines.Add(new StyledLine().Add($"{Indent(heading.Rank)}▌ {TextSanitizer.Expand(heading.Title)}", style));

        // Children sit one level below the heading, exactly like in the plain-text visitor.
        var previousDepth = _depth;
        _depth = heading.Rank + 1;
        VisitChildren(heading);
        _depth = previousDepth;
    }

    public void Visit(Paragraph paragraph)
    {
        var indent = Indent(_depth);
        foreach (var chunk in WordWrap(paragraph.Title, _width - indent.Length - 1))
        {
            _lines.Add(new StyledLine().Add(indent + chunk, Palette.Text));
        }

        AddBlankLine();
    }

    public void Visit(Link link)
    {
        _lines.Add(new StyledLine()
            .Add(Indent(_depth) + "↗ ", Palette.Dim)
            .Add(TextSanitizer.Expand(link.Title), Palette.LinkTitle)
            .Add("  " + TextSanitizer.Expand(link.Url), Palette.Dim));
        AddBlankLine();
    }

    /// <summary>Breaks text into rows of at most <paramref name="width"/> characters, cutting only between words when possible.</summary>
    public static IEnumerable<string> WordWrap(string text, int width)
    {
        width = Math.Max(1, width);
        var current = new StringBuilder();

        foreach (var rawWord in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var word = TextSanitizer.Expand(rawWord);

            // A single word longer than the row is cut hard.
            while (word.Length > width)
            {
                if (current.Length > 0)
                {
                    yield return current.ToString();
                    current.Clear();
                }

                yield return word[..width];
                word = word[width..];
            }

            if (current.Length > 0 && current.Length + 1 + word.Length > width)
            {
                yield return current.ToString();
                current.Clear();
            }

            if (current.Length > 0)
            {
                current.Append(' ');
            }

            current.Append(word);
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    private void VisitChildren(Container container)
    {
        foreach (var child in container.Children)
        {
            child.Accept(this);
        }
    }

    private void AddBlankLine()
    {
        if (_lines.Count > 0 && _lines[^1].Length > 0)
        {
            _lines.Add(new StyledLine());
        }
    }

    // One cell of left margin plus two cells per nesting level.
    private static string Indent(int depth) => new(' ', 1 + depth * 2);
}
