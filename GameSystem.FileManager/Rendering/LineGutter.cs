using System.Globalization;

namespace GameSystem.FileManager.Rendering;

/// <summary>The line-number column shared by the view and edit modes.</summary>
public static class LineGutter
{
    private const int MinDigits = 3;
    private const int SeparatorWidth = 3; // " │ "

    /// <summary>Total width of the gutter in cells.</summary>
    public static int Width(int totalLines) =>
        Math.Max(MinDigits, totalLines.ToString(CultureInfo.InvariantCulture).Length) + SeparatorWidth;

    /// <summary>Creates a row that already contains the gutter; the caller appends the text.</summary>
    public static StyledLine Create(int lineNumber, int totalLines, bool isCurrent)
    {
        var digits = Width(totalLines) - SeparatorWidth;
        var numberStyle = isCurrent ? Palette.LineNumberCurrent : Palette.LineNumber;
        return new StyledLine()
            .Add(lineNumber.ToString(CultureInfo.InvariantCulture).PadLeft(digits), numberStyle)
            .Add(" │ ", Palette.Separator);
    }
}
