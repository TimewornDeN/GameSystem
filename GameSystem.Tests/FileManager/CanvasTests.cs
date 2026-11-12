using Xunit;
using GameSystem.FileManager.Rendering;
using GameSystem.Tests.FileManager.Support;
using GameSystem.Tests.TestDoubles;

namespace GameSystem.Tests.FileManager;

public class CanvasTests
{
    [Fact]
    public void BodyHeight_IsHeightMinusHeaderAndFooter()
    {
        Assert.Equal(6, new Canvas(40, 10).BodyHeight);
    }

    [Fact]
    public void Compose_ProducesExactlyWidthTimesHeightVisibleCells()
    {
        var canvas = new Canvas(20, 8);
        canvas.Title = new StyledLine("a title that is much longer than twenty cells", new Style(Foreground: 1));
        canvas.Body.Add(new StyledLine("short"));

        var visible = TestText.StripAnsi(canvas.Compose());

        Assert.Equal(20 * 8, visible.Length);
    }

    [Fact]
    public void Compose_HidesTheCursorByDefault()
    {
        var frame = new Canvas(20, 8).Compose();

        Assert.Contains(Ansi.HideCursor, frame, StringComparison.Ordinal);
        Assert.DoesNotContain(Ansi.ShowCursor, frame, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_PlacesTheCursorInsideTheBody()
    {
        var canvas = new Canvas(20, 8) { Cursor = (2, 7) };

        var frame = canvas.Compose();

        // Body starts on screen row 3, so body row 2 is row 5; columns are 1-based.
        Assert.Contains(Ansi.MoveTo(5, 8) + Ansi.ShowCursor, frame, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_ContainsColorCodes()
    {
        var canvas = new Canvas(20, 8);
        canvas.Body.Add(new StyledLine("hi", new Style(Foreground: 114)));

        Assert.Contains("\u001b[38;5;114m", canvas.Compose(), StringComparison.Ordinal);
    }
}
