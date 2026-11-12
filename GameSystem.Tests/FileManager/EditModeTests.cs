using Xunit;
using GameSystem.FileManager.Modes;
using GameSystem.FileManager.Rendering;
using GameSystem.Tests.FileManager.Support;
using GameSystem.Tests.TestDoubles;

namespace GameSystem.Tests.FileManager;

public class EditModeTests
{
    [Fact]
    public void TypedCharacters_AreInsertedAtTheCursor()
    {
        var harness = new AppHarness();
        harness.OpenNotesInEditor();

        harness.Send(Keys.Text('X'));

        Assert.Contains("Xalpha", harness.Screen);
        Assert.Contains("modified", harness.Screen);
    }

    [Fact]
    public void EditMode_ShowsTheCursor_OtherModesHideIt()
    {
        var harness = new AppHarness();
        _ = harness.Screen;
        Assert.DoesNotContain(Ansi.ShowCursor, harness.Terminal.LastFrame, StringComparison.Ordinal);

        harness.OpenNotesInEditor();
        _ = harness.Screen;
        Assert.Contains(Ansi.ShowCursor, harness.Terminal.LastFrame, StringComparison.Ordinal);
    }

    [Fact]
    public void CtrlS_WritesTheFile()
    {
        var harness = new AppHarness();
        harness.OpenNotesInEditor();

        harness.Send(Keys.Text('X'), Keys.CtrlS);

        Assert.Equal("Xalpha", harness.Files.GetFileContent("/root/notes.txt")[0]);
        Assert.Contains("Saved notes.txt", harness.Screen);
    }

    [Fact]
    public void Escape_WithUnsavedChanges_AsksBeforeDiscarding()
    {
        var harness = new AppHarness();
        harness.OpenNotesInEditor();
        harness.Send(Keys.Text('X'));

        harness.Send(Keys.Escape);
        Assert.True(harness.IsIn<EditMode>());
        Assert.Contains("Unsaved changes", harness.Screen);

        harness.Send(Keys.Escape);
        Assert.True(harness.IsIn<FileViewMode>());
        Assert.Equal("alpha", harness.Files.GetFileContent("/root/notes.txt")[0]);
        Assert.Contains("alpha", harness.Screen);
        Assert.DoesNotContain("Xalpha", harness.Screen);
    }

    [Fact]
    public void AnyOtherKey_CancelsThePendingDiscard()
    {
        var harness = new AppHarness();
        harness.OpenNotesInEditor();
        harness.Send(Keys.Text('X'), Keys.Escape, Keys.Right);

        harness.Send(Keys.Escape);

        Assert.True(harness.IsIn<EditMode>());
    }

    [Fact]
    public void Escape_AfterSaving_ReturnsToViewWithTheNewContent()
    {
        var harness = new AppHarness();
        harness.OpenNotesInEditor();

        harness.Send(Keys.Text('X'), Keys.CtrlS, Keys.Escape);

        Assert.True(harness.IsIn<FileViewMode>());
        Assert.Contains("Xalpha", harness.Screen);
    }

    [Fact]
    public void FailedSave_ShowsErrorAndKeepsTheChanges()
    {
        var harness = new AppHarness();
        harness.OpenNotesInEditor();
        harness.Files.ThrowOnWrite = true;

        harness.Send(Keys.Text('X'), Keys.CtrlS);

        Assert.Contains("Cannot save", harness.Screen);
        harness.Send(Keys.Escape);
        Assert.True(harness.IsIn<EditMode>());
    }

    [Fact]
    public void Enter_SplitsTheCurrentLine()
    {
        var harness = new AppHarness();
        harness.OpenNotesInEditor();

        harness.Send(Keys.Right, Keys.Enter, Keys.CtrlS);

        Assert.Equal("a|lpha|beta", string.Join('|', harness.Files.GetFileContent("/root/notes.txt")));
    }
}
