using System.Text.RegularExpressions;

namespace GameSystem.Tests.FileManager.Support;

internal static partial class TestText
{
    /// <summary>Removes ANSI escape sequences so tests can assert on the visible text.</summary>
    public static string StripAnsi(string text) => AnsiPattern().Replace(text, string.Empty);

    [GeneratedRegex("\u001b\\[[0-9;?]*[A-Za-z]")]
    private static partial Regex AnsiPattern();
}
