using Xunit;
using GameSystem.FileManager.Editing;

namespace GameSystem.Tests.FileManager;

public class ViewportTests
{
    [Fact]
    public void EnsureVisible_ScrollsDownJustEnough()
    {
        var viewport = new Viewport();

        viewport.EnsureVisible(5, height: 3, total: 10);

        Assert.Equal(3, viewport.Top);
    }

    [Fact]
    public void EnsureVisible_ScrollsUp()
    {
        var viewport = new Viewport();
        viewport.ScrollTo(6, 3, 10);

        viewport.EnsureVisible(2, 3, 10);

        Assert.Equal(2, viewport.Top);
    }

    [Fact]
    public void ScrollTo_IsClampedToTheContent()
    {
        var viewport = new Viewport();

        viewport.ScrollTo(int.MaxValue, 3, 10);
        Assert.Equal(7, viewport.Top);

        viewport.ScrollBy(-100, 3, 10);
        Assert.Equal(0, viewport.Top);
    }

    [Fact]
    public void ShortContent_NeverScrolls()
    {
        var viewport = new Viewport();

        viewport.ScrollBy(5, height: 10, total: 4);

        Assert.Equal(0, viewport.Top);
    }
}
