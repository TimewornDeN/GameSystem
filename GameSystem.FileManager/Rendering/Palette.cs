namespace GameSystem.FileManager.Rendering;

/// <summary>All colors of the application in one place, so the look can be changed without touching the modes.</summary>
public static class Palette
{
    // Title bar and mode badges
    public static readonly Style TitleBar = new(Foreground: 255, Background: 236);
    public static readonly Style AppName = new(Foreground: 16, Background: 213, Bold: true);
    public static readonly Style DirectoryBadge = new(Foreground: 16, Background: 45, Bold: true);
    public static readonly Style ViewBadge = new(Foreground: 16, Background: 221, Bold: true);
    public static readonly Style EditBadge = new(Foreground: 16, Background: 209, Bold: true);

    // General text
    public static readonly Style Path = new(Foreground: 81);
    public static readonly Style Dim = new(Foreground: 244);
    public static readonly Style Separator = new(Foreground: 240);
    public static readonly Style Text = new(Foreground: 252);
    public static readonly Style Modified = new(Foreground: 209, Bold: true);
    public static readonly Style Saved = new(Foreground: 78);

    // Directory listing
    public static readonly Style Directory = new(Foreground: 75, Bold: true);
    public static readonly Style Size = new(Foreground: 108);
    public static readonly Style Selected = new(Foreground: 255, Background: 24, Bold: true);

    // File content
    public static readonly Style LineNumber = new(Foreground: 240);
    public static readonly Style LineNumberCurrent = new(Foreground: 221, Bold: true);
    public static readonly Style CurrentLine = new(Foreground: 255, Background: 237);

    // Status line
    public static readonly Style Info = new(Foreground: 111);
    public static readonly Style Success = new(Foreground: 78, Bold: true);
    public static readonly Style Error = new(Foreground: 203, Bold: true);
    public static readonly Style Warning = new(Foreground: 221, Bold: true);

    // Formatted (structured text) view
    public static readonly Style DocumentTitle = new(Foreground: 255, Background: 54, Bold: true);
    public static readonly Style LinkTitle = new(Foreground: 117, Underline: true);

    // Key hints
    public static readonly Style HintKey = new(Foreground: 221, Bold: true);
    public static readonly Style HintText = new(Foreground: 245);

    /// <summary>Picks a color for a file by its extension.</summary>
    public static Style ForFile(string fileName) => System.IO.Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".cs" => new Style(Foreground: 114),
        ".csproj" or ".sln" or ".slnx" or ".props" => new Style(Foreground: 141),
        ".json" or ".xml" or ".yml" or ".yaml" => new Style(Foreground: 221),
        ".md" or ".txt" => new Style(Foreground: 183),
        ".png" or ".jpg" or ".jpeg" or ".gif" or ".svg" => new Style(Foreground: 213),
        ".exe" or ".dll" => new Style(Foreground: 203),
        ".zip" or ".7z" or ".tar" or ".gz" => new Style(Foreground: 208),
        _ => new Style(Foreground: 252),
    };
}
