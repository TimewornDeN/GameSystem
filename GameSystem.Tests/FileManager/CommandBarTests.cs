using GameSystem.FileManager.Modes;
using GameSystem.FileManager.Rendering;
using GameSystem.Tests.FileManager.Support;
using Xunit;

namespace GameSystem.Tests.FileManager;

/// <summary>The ":" command bar reuses the CLI parser/registry and reports through IDisplayer.</summary>
public class CommandBarTests
{
    private static void RunCommand(AppHarness harness, string command)
    {
        harness.Send(Keys.Text(':'));
        harness.Type(command);
        harness.Send(Keys.Enter);
    }

    [Fact]
    public void Cd_ChangesTheDirectory()
    {
        var harness = new AppHarness();

        RunCommand(harness, "cd docs");

        var screen = harness.Screen;
        Assert.Contains("/root/docs", screen);
        Assert.Contains("todo.txt", screen);
    }

    [Fact]
    public void Cd_DotDot_GoesUp()
    {
        var harness = new AppHarness();
        harness.Send(Keys.Enter); // into docs

        RunCommand(harness, "cd ..");

        Assert.Contains("readme.md", harness.Screen);
    }

    [Fact]
    public void Cd_ToMissingDirectory_ShowsErrorWithIcon()
    {
        var harness = new AppHarness();

        RunCommand(harness, "cd nowhere");

        var screen = harness.Screen;
        Assert.Contains("✖ Cannot open", screen);
        Assert.Contains("notes.txt", screen); // the old listing stays
    }

    [Fact]
    public void Cd_WithoutArgument_ShowsUsage()
    {
        var harness = new AppHarness();

        RunCommand(harness, "cd");

        Assert.Contains("Usage: cd <path>", harness.Screen);
    }

    [Fact]
    public void Open_File_SwitchesToViewMode()
    {
        var harness = new AppHarness();

        RunCommand(harness, "open notes.txt");

        Assert.True(harness.IsIn<FileViewMode>());
    }

    [Fact]
    public void Open_WithEditOption_GoesStraightToTheEditor()
    {
        var harness = new AppHarness();

        RunCommand(harness, "open notes.txt --edit");

        Assert.True(harness.IsIn<EditMode>());
    }

    [Fact]
    public void Open_QuotedName_IsHandledByTheCliParser()
    {
        var harness = new AppHarness(files => files.AddFile("/root", "my notes.txt", "hello"));

        RunCommand(harness, "open \"my notes.txt\"");

        Assert.True(harness.IsIn<FileViewMode>());
        Assert.Contains("hello", harness.Screen);
    }

    [Fact]
    public void Open_MissingName_ShowsError()
    {
        var harness = new AppHarness();

        RunCommand(harness, "open nope.txt");

        Assert.Contains("was not found", harness.Screen);
    }

    [Fact]
    public void UnknownCommand_ShowsWarningFromTheRegistry()
    {
        var harness = new AppHarness();

        RunCommand(harness, "dance");

        var screen = harness.Screen;
        Assert.Contains("⚠ Unknown command 'dance'", screen);
        Assert.True(harness.IsIn<DirectoryMode>());
    }

    [Fact]
    public void Help_ListsTheCommandsOfTheCurrentMode()
    {
        var harness = new AppHarness();

        RunCommand(harness, "help");

        var screen = harness.Screen;
        Assert.Contains("ℹ Commands:", screen);
        Assert.Contains("cd", screen);
        Assert.Contains("open", screen);
        Assert.Contains("quit", screen);
    }

    [Fact]
    public void Quit_ExitsTheApplication()
    {
        var harness = new AppHarness();

        RunCommand(harness, "quit");

        Assert.True(harness.App.IsExitRequested);
    }

    [Fact]
    public void Escape_CancelsTheBarWithoutRunningAnything()
    {
        var harness = new AppHarness();
        harness.Send(Keys.Text(':'));
        harness.Type("quit");

        harness.Send(Keys.Escape);
        Assert.False(harness.App.IsExitRequested);

        harness.Send(Keys.Escape); // now it is the normal Esc of the directory mode
        Assert.True(harness.App.IsExitRequested);
    }

    [Fact]
    public void Backspace_OnAnEmptyBar_ClosesIt()
    {
        var harness = new AppHarness();

        harness.Send(Keys.Text(':'), Keys.Backspace);
        harness.Send(Keys.Escape);

        Assert.True(harness.App.IsExitRequested);
    }

    [Fact]
    public void Bar_ShowsPromptAndPutsTheCursorInTheStatusRow()
    {
        var harness = new AppHarness();
        harness.Send(Keys.Text(':'));
        harness.Type("cd");

        var screen = harness.Screen;

        Assert.Contains(":cd", screen);
        Assert.Contains(
            Ansi.MoveTo(harness.Terminal.Height - 1, 4) + Ansi.ShowCursor,
            harness.Terminal.LastFrame,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ViewMode_Goto_ScrollsToTheLine()
    {
        var harness = new AppHarness(files =>
            files.AddFile("/root", "long.txt", Enumerable.Range(1, 10).Select(i => $"line {i}").ToArray()));
        harness.Terminal.Height = 7; // 3 body rows
        harness.Send(Keys.Down, Keys.Down, Keys.Enter);

        RunCommand(harness, "goto 8");

        var screen = harness.Screen;
        Assert.Contains("line 8", screen);
        Assert.Contains("Lines 8-10 of 10", screen);
    }

    [Fact]
    public void ViewMode_EditAndCloseCommands_SwitchModes()
    {
        var harness = new AppHarness();
        harness.OpenNotes();

        RunCommand(harness, "edit");
        Assert.True(harness.IsIn<EditMode>());

        harness.Send(Keys.Escape);
        RunCommand(harness, "close");
        Assert.True(harness.IsIn<DirectoryMode>());
    }

    [Fact]
    public void EditMode_TreatsTheColonAsNormalText()
    {
        var harness = new AppHarness();
        harness.OpenNotesInEditor();

        harness.Send(Keys.Text(':'));

        Assert.True(harness.IsIn<EditMode>());
        Assert.Contains(":alpha", harness.Screen);
    }
}
