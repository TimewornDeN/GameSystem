using System.Text.RegularExpressions;
using GameSystem.Core.Text;

namespace GameSystem.FileManager.Rendering;

/// <summary>
/// Turns a small Markdown subset into the structured-text tree of the Text module
/// (Root / Heading / Paragraph / Link), so the existing document model is reused instead of a new one.
/// Supported: <c>#</c>..<c>######</c> headings, paragraphs separated by blank lines,
/// and a paragraph that is exactly <c>[title](url)</c> becomes a link.
/// </summary>
public static partial class MarkdownDocumentParser
{
    public static Root Parse(string title, IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var root = new Root(string.IsNullOrWhiteSpace(title) ? "Document" : title);
        var open = new Stack<Container>();
        open.Push(root);
        var paragraph = new List<string>();

        void FlushParagraph()
        {
            if (paragraph.Count == 0)
            {
                return;
            }

            var text = string.Join(' ', paragraph);
            paragraph.Clear();

            var link = LinkPattern().Match(text);
            open.Peek().AddChild(link.Success
                ? (IText)new Link(link.Groups["title"].Value, link.Groups["url"].Value)
                : new Paragraph(text));
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                FlushParagraph();
                continue;
            }

            var match = HeadingPattern().Match(line);
            if (!match.Success)
            {
                paragraph.Add(line);
                continue;
            }

            FlushParagraph();
            var rank = match.Groups[1].Length - 1;

            // A heading belongs to the closest enclosing heading of a lower rank (or to the root).
            while (open.Peek() is Heading top && top.Rank >= rank)
            {
                open.Pop();
            }

            var heading = new Heading(match.Groups[2].Value, rank);
            open.Peek().AddChild(heading);
            open.Push(heading);
        }

        FlushParagraph();
        return root;
    }

    [GeneratedRegex(@"^(#{1,6})\s+(\S.*)$")]
    private static partial Regex HeadingPattern();

    [GeneratedRegex(@"^\[(?<title>[^\]]+)\]\((?<url>[^)\s]+)\)$")]
    private static partial Regex LinkPattern();
}
