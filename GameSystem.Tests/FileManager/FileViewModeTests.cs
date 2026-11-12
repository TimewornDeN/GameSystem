using Xunit;
using GameSystem.FileManager.Modes;
using GameSystem.Tests.FileManager.Support;
using GameSystem.Tests.TestDoubles;

namespace GameSystem.Tests.FileManager;

public class FileViewModeTests
{
    [Fact]
    public void ShowsFileContentWithLineNumbers()
    {
        var harness = new AppHarness();

        harness.OpenNotes();

        var screen = harness.Screen;
        Assert.Contains("/root/notes.txt", screen);
        Assert.Contains("alpha", screen);
        Assert.Contains("beta", screen);
        Assert.Contains("  2 │ beta", screen);
    }

    [Fact]
    public void Scrolling_MovesTheVisibleWindow()
    {
        var harness = new AppHarness(files =>
            files.AddFile("/root", "long.txt", Enumerable.Range(1, 10).Select(i => $"line {i}").ToArray()));
        harness.Terminal.Height = 7; // 3 body rows
        harness.Send(Keys.Down, Keys.Down, Keys.Enter); // long.txt is third in the list

        Assert.Contains("line 1", harness.Screen);
        Assert.DoesNotContain("line 4", harness.Screen);
        Assert.Contains("Lines 1-3 of 10", harness.Screen);

        harness.Send(Keys.PageDown);
        Assert.Contains("line 4", harness.Screen);
        Assert.DoesNotContain("line 1", harness.Screen);

        harness.Send(Keys.End);
        Assert.Contains("line 10", harness.Screen);
        Assert.Contains("Lines 8-10 of 10", harness.Screen);
    }

    [Fact]
    public void Q_ClosesTheFileAndSelectsItInTheDirectory()
    {
        var harness = new AppHarness();
        harness.OpenNotes();

        harness.Send(Keys.Q);

        Assert.True(harness.IsIn<DirectoryMode>());
        Assert.Contains("notes.txt  ·  3/4", harness.Screen);
    }

    [Fact]
    public void E_SwitchesToEditMode()
    {
        var harness = new AppHarness();
        harness.OpenNotes();

        harness.Send(Keys.E);

        Assert.True(harness.IsIn<EditMode>());
    }

    [Fact]
    public void ControlCharactersInFilesAreNeverSentToTheTerminal()
    {
        var harness = new AppHarness(files => files.AddFile("/root", "evil.txt", "\tx\u001b[31mred"));
        harness.Send(Keys.Down, Keys.Down, Keys.Enter); // evil.txt sorts before notes.txt

        _ = harness.Screen;

        Assert.DoesNotContain("\u001b[31m", harness.Terminal.LastFrame, StringComparison.Ordinal);
        Assert.Contains("·[31mred", TestText.StripAnsi(harness.Terminal.LastFrame));
    }

    [Fact]
    public void EmptyFile_ShowsPlaceholder()
    {
        var harness = new AppHarness(files => files.AddFile("/root", "empty.txt"));
        harness.Send(Keys.Down, Keys.Down, Keys.Enter);

        Assert.Contains("(empty file)", harness.Screen);
    }
}
