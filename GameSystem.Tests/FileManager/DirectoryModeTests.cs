using Xunit;
using GameSystem.FileManager.Modes;
using GameSystem.Tests.FileManager.Support;
using GameSystem.Tests.TestDoubles;

namespace GameSystem.Tests.FileManager;

public class DirectoryModeTests
{
    [Fact]
    public void Listing_ShowsDirectoriesBeforeFiles()
    {
        var screen = new AppHarness().Screen;

        Assert.Contains("/root", screen);
        Assert.True(screen.IndexOf("docs", StringComparison.Ordinal) < screen.IndexOf("notes.txt", StringComparison.Ordinal));
        Assert.Contains("readme.md", screen);
    }

    [Fact]
    public void Enter_OnDirectory_ShowsItsContentAndParentEntry()
    {
        var harness = new AppHarness();

        harness.Send(Keys.Enter);

        Assert.True(harness.IsIn<DirectoryMode>());
        var screen = harness.Screen;
        Assert.Contains("/root/docs", screen);
        Assert.Contains("todo.txt", screen);
        Assert.Contains("..", screen);
    }

    [Fact]
    public void Enter_OnParentEntry_GoesBack()
    {
        var harness = new AppHarness();

        harness.Send(Keys.Enter, Keys.Enter);

        Assert.Contains("notes.txt", harness.Screen);
    }

    [Fact]
    public void Backspace_GoesUpAndSelectsTheDirectoryWeCameFrom()
    {
        var harness = new AppHarness();

        harness.Send(Keys.Enter, Keys.Backspace);

        Assert.Contains("docs  ·  1/4", harness.Screen);
    }

    [Fact]
    public void Backspace_AtRoot_ShowsInfo()
    {
        var harness = new AppHarness();

        harness.Send(Keys.Backspace);

        Assert.Contains("Already at the root", harness.Screen);
    }

    [Fact]
    public void Enter_OnUnreadableDirectory_ShowsErrorAndKeepsTheListing()
    {
        var harness = new AppHarness();

        harness.Send(Keys.Down, Keys.Enter);

        var screen = harness.Screen;
        Assert.True(harness.IsIn<DirectoryMode>());
        Assert.Contains("Cannot open", screen);
        Assert.Contains("notes.txt", screen);
    }

    [Fact]
    public void Enter_OnFile_OpensViewMode()
    {
        var harness = new AppHarness();

        harness.OpenNotes();

        Assert.True(harness.IsIn<FileViewMode>());
    }

    [Fact]
    public void Selection_IsClampedAtBothEnds()
    {
        var harness = new AppHarness();

        harness.Send(Keys.Up);
        Assert.Contains("docs  ·  1/4", harness.Screen);

        harness.Send(Keys.End, Keys.Down);
        Assert.Contains("readme.md  ·  4/4", harness.Screen);
    }

    [Fact]
    public void Escape_ExitsTheApplication()
    {
        var harness = new AppHarness();

        harness.Send(Keys.Escape);

        Assert.True(harness.App.IsExitRequested);
    }
}
