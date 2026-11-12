namespace GameSystem.FileManager.Rendering;

/// <summary>
/// Visual style of a piece of text. Colors are indexes of the xterm 256-color palette,
/// <c>null</c> keeps the terminal default.
/// </summary>
public readonly record struct Style(
    int? Foreground = null,
    int? Background = null,
    bool Bold = false,
    bool Underline = false,
    bool Inverse = false)
{
    /// <summary>Builds the SGR escape sequence for this style, or an empty string for the default style.</summary>
    public string ToAnsi()
    {
        var codes = new List<string>();
        if (Bold) codes.Add("1");
        if (Underline) codes.Add("4");
        if (Inverse) codes.Add("7");
        if (Foreground is { } foreground) codes.Add($"38;5;{foreground}");
        if (Background is { } background) codes.Add($"48;5;{background}");
        return codes.Count == 0 ? string.Empty : $"\u001b[{string.Join(';', codes)}m";
    }
}
