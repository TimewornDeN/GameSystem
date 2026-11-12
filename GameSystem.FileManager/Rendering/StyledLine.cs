using System.Text;

namespace GameSystem.FileManager.Rendering;

/// <summary>One screen row made of differently styled spans.</summary>
public sealed class StyledLine
{
    private readonly List<TextSpan> _spans = [];

    public StyledLine()
    {
    }

    public StyledLine(string text, Style style = default) => Add(text, style);

    public IReadOnlyList<TextSpan> Spans => _spans;

    /// <summary>Number of visible characters (escape sequences are not counted).</summary>
    public int Length => _spans.Sum(span => span.Text.Length);

    public StyledLine Add(string text, Style style = default)
    {
        if (text.Length > 0)
        {
            _spans.Add(new TextSpan(text, style));
        }

        return this;
    }

    /// <summary>
    /// Renders the row exactly <paramref name="width"/> cells wide: long rows are cut,
    /// short rows are padded with spaces drawn in <paramref name="padStyle"/>.
    /// </summary>
    public string ToAnsi(int width, Style padStyle = default)
    {
        var builder = new StringBuilder();
        var remaining = width;

        foreach (var span in _spans)
        {
            if (remaining <= 0)
            {
                break;
            }

            var text = span.Text.Length <= remaining ? span.Text : span.Text[..remaining];
            builder.Append(span.Style.ToAnsi()).Append(text).Append(Ansi.Reset);
            remaining -= text.Length;
        }

        if (remaining > 0)
        {
            builder.Append(padStyle.ToAnsi()).Append(' ', remaining).Append(Ansi.Reset);
        }

        return builder.ToString();
    }
}
