using Xunit;
using GameSystem.FileManager.Modes;
using GameSystem.Tests.FileManager.Support;
using GameSystem.Tests.TestDoubles;

namespace GameSystem.Tests.FileManager;

public class AppFlowTests
{
    [Fact]
    public void FullDiagramPath_DirectoryToViewToEditAndBack()
    {
        var harness = new AppHarness();

        harness.OpenNotes();
        Assert.True(harness.IsIn<FileViewMode>());

        harness.Send(Keys.E);
        Assert.True(harness.IsIn<EditMode>());

        harness.Send(Keys.Text('>'), Keys.Text(' '), Keys.CtrlS);
        harness.Send(Keys.Escape);
        Assert.True(harness.IsIn<FileViewMode>());
        Assert.Contains("> alpha", harness.Screen);

        harness.Send(Keys.Q);
        Assert.True(harness.IsIn<DirectoryMode>());

        harness.Send(Keys.Escape);
        Assert.True(harness.App.IsExitRequested);
        Assert.Equal("> alpha", harness.Files.GetFileContent("/root/notes.txt")[0]);
    }

    [Fact]
    public void Run_OpensAndRestoresTheTerminal()
    {
        var harness = new AppHarness();
        harness.Terminal.Enqueue(Keys.Escape);

        harness.App.Run();

        Assert.Equal(1, harness.Terminal.OpenCount);
        Assert.False(harness.Terminal.IsOpen);
    }

    [Fact]
    public void SameKeyMeansDifferentThingsInDifferentModes()
    {
        var harness = new AppHarness();

        // Enter opens a file in the directory mode...
        harness.OpenNotes();
        Assert.True(harness.IsIn<FileViewMode>());

        // ...is ignored in the view mode...
        harness.Send(Keys.Enter);
        Assert.True(harness.IsIn<FileViewMode>());

        // ...and splits a line in the edit mode.
        harness.Send(Keys.E, Keys.Enter);
        Assert.Contains("modified", harness.Screen);
    }
}
