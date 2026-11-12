namespace GameSystem.CLI.Graphics;

/// <summary>Text progress bar, for example a health bar: <c>[#####-----]</c>.</summary>
public static class ProgressBar
{
    public static string Render(int value, int max, int width = 10)
    {
        width = Math.Max(1, width);

        int filled = max <= 0
            ? 0
            : (int)Math.Round((double)Math.Clamp(value, 0, max) / max * width, MidpointRounding.AwayFromZero);

        return "[" + new string('#', filled) + new string('-', width - filled) + "]";
    }
}
