using System.Text;

namespace GameSystem.FileManager.Rendering;

/// <summary>
/// Makes file content safe to print: a file may contain control characters (including ESC),
/// which would otherwise be interpreted by the terminal as escape sequences.
/// </summary>
public static class TextSanitizer
{
    public const int TabWidth = 4;
    private const char ControlPlaceholder = '·';

    /// <summary>For read-only display: tabs become spaces, other control characters become a placeholder.</summary>
    public static string Expand(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            if (character == '\t')
            {
                builder.Append(' ', TabWidth);
            }
            else
            {
                builder.Append(char.IsControl(character) ? ControlPlaceholder : character);
            }
        }

        return builder.ToString();
    }

    /// <summary>For editing: keeps a strict 1 character = 1 cell mapping so the cursor column stays exact.</summary>
    public static string Flatten(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            builder.Append(character == '\t' ? '→' : char.IsControl(character) ? ControlPlaceholder : character);
        }

        return builder.ToString();
    }
}
