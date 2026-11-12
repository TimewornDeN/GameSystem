using GameSystem.Core.Abstractions;
using GameSystem.Core.Models;
using GameSystem.Infra.Display;
using Xunit;

namespace GameSystem.Tests.Infra;

public class TextWriterDisplayerTests
{
    [Theory]
    [InlineData(DisplayKind.Plain, "")]
    [InlineData(DisplayKind.Info, "[Info]: ")]
    [InlineData(DisplayKind.Success, "[Success]: ")]
    [InlineData(DisplayKind.Warning, "[Warning]: ")]
    [InlineData(DisplayKind.Error, "[Error]: ")]
    public void Display_WritesExpectedPrefix(DisplayKind kind, string expectedPrefix)
    {
        using var writer = new StringWriter();
        var displayer = new TextWriterDisplayer(writer);

        displayer.Display(new Message("hello", kind));

        Assert.Equal(expectedPrefix + "hello" + writer.NewLine, writer.ToString());
    }

    [Fact]
    public void Display_WritesEachMessageOnItsOwnLine()
    {
        using var writer = new StringWriter();
        var displayer = new TextWriterDisplayer(writer);

        displayer.Show("first");
        displayer.Show("second");

        var lines = writer.ToString().Split(writer.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(["first", "second"], lines);
    }

    [Fact]
    public void Constructor_RejectsNullWriter()
    {
        Assert.Throws<ArgumentNullException>(() => new TextWriterDisplayer(null!));
    }
}
