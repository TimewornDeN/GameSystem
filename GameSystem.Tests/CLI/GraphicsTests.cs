using GameSystem.CLI.Graphics;
using Xunit;

namespace GameSystem.Tests.CLI;

public class GraphicsTests
{
    [Theory]
    [InlineData(50, 100, 10, "[#####-----]")]
    [InlineData(0, 100, 10, "[----------]")]
    [InlineData(100, 100, 10, "[##########]")]
    [InlineData(-5, 100, 10, "[----------]")]
    [InlineData(500, 100, 10, "[##########]")]
    [InlineData(10, 0, 10, "[----------]")]
    [InlineData(1, 2, 4, "[##--]")]
    public void ProgressBar_FillsProportionally(int value, int max, int width, string expected)
    {
        Assert.Equal(expected, ProgressBar.Render(value, max, width));
    }

    [Fact]
    public void ConsoleTable_AlignsColumns_SoEveryLineHasTheSameLength()
    {
        var table = new ConsoleTable("Name", "HP").AddRow("Grosh", "150/150").AddRow("El", "7/80");

        var lines = table.ToString().Split(Environment.NewLine);

        Assert.Equal(6, lines.Length); // separator, header, separator, 2 rows, closing separator
        Assert.Single(lines.Select(l => l.Length).Distinct());
        Assert.Contains("| Grosh | 150/150 |", lines);
        Assert.Contains("| El    | 7/80    |", lines);
    }

    [Fact]
    public void ConsoleTable_WithoutRows_StillShowsTheHeader()
    {
        var text = new ConsoleTable("A", "B").ToString();

        Assert.Contains("| A | B |", text);
        Assert.Equal(0, new ConsoleTable("A").RowCount);
    }

    [Fact]
    public void ConsoleTable_RowWithWrongNumberOfCells_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ConsoleTable("A", "B").AddRow("only one"));
    }

    [Fact]
    public void ConsoleTable_WithoutColumns_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ConsoleTable());
    }
}
