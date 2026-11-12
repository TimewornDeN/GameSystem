using Xunit;
using GameSystem.FileManager.Rendering;
using GameSystem.Tests.FileManager.Support;
using GameSystem.Tests.TestDoubles;

namespace GameSystem.Tests.FileManager;

public class StyledLineTests
{
    [Fact]
    public void ToAnsi_TruncatesToWidth()
    {
        var line = new StyledLine("Hello world");

        Assert.Equal("Hello", TestText.StripAnsi(line.ToAnsi(5)));
    }

    [Fact]
    public void ToAnsi_PadsToWidth()
    {
        var line = new StyledLine("Hi");

        Assert.Equal("Hi      ", TestText.StripAnsi(line.ToAnsi(8)));
    }

    [Fact]
    public void Length_IgnoresStyles()
    {
        var line = new StyledLine().Add("ab", new Style(Foreground: 1)).Add("cde", new Style(Bold: true));

        Assert.Equal(5, line.Length);
    }

    [Fact]
    public void Style_BuildsCombinedSequence()
    {
        var style = new Style(Foreground: 10, Background: 20, Bold: true);

        Assert.Equal("\u001b[1;38;5;10;48;5;20m", style.ToAnsi());
        Assert.Equal(string.Empty, default(Style).ToAnsi());
    }

    [Fact]
    public void Sanitizer_RemovesEscapeCharacters()
    {
        Assert.DoesNotContain('\u001b', TextSanitizer.Expand("a\u001b[0mb"));
        Assert.DoesNotContain('\u001b', TextSanitizer.Flatten("a\u001b[0mb"));
        Assert.Equal("a    b", TextSanitizer.Expand("a\tb"));
        Assert.Equal(3, TextSanitizer.Flatten("a\tb").Length);
    }
}
