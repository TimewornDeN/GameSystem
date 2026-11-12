namespace GameSystem.FileManager.Editing;

/// <summary>Keeps track of which slice of a long list or text is visible.</summary>
public sealed class Viewport
{
    public int Top { get; private set; }

    public void Reset() => Top = 0;

    public void Clamp(int height, int total)
    {
        var max = Math.Max(0, total - Math.Max(1, height));
        Top = Math.Clamp(Top, 0, max);
    }

    public void ScrollBy(int delta, int height, int total) => ScrollTo(Top + delta, height, total);

    public void ScrollTo(int top, int height, int total)
    {
        Top = top;
        Clamp(height, total);
    }

    /// <summary>Scrolls the minimum amount needed to bring <paramref name="index"/> into view.</summary>
    public void EnsureVisible(int index, int height, int total)
    {
        height = Math.Max(1, height);
        if (index < Top)
        {
            Top = index;
        }
        else if (index >= Top + height)
        {
            Top = index - height + 1;
        }

        Clamp(height, total);
    }
}
