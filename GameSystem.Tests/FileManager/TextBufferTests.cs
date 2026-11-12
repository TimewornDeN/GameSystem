using Xunit;
using GameSystem.FileManager.Editing;

namespace GameSystem.Tests.FileManager;

public class TextBufferTests
{
    [Fact]
    public void EmptyInput_StillHasOneLine()
    {
        var buffer = new TextBuffer([]);

        Assert.Single(buffer.Lines);
        Assert.Equal(string.Empty, buffer.Lines[0]);
    }

    [Fact]
    public void Insert_AddsCharacterAndMovesCursor()
    {
        var buffer = new TextBuffer(["ac"]);
        buffer.MoveRight();

        buffer.Insert('b');

        Assert.Equal("abc", buffer.Lines[0]);
        Assert.Equal(2, buffer.Column);
        Assert.True(buffer.IsDirty);
    }

    [Fact]
    public void InsertNewLine_SplitsTheLine()
    {
        var buffer = new TextBuffer(["abcd"]);
        buffer.MoveRight();
        buffer.MoveRight();

        buffer.InsertNewLine();

        Assert.Equal("ab|cd", string.Join('|', buffer.Lines));
        Assert.Equal((1, 0), (buffer.Row, buffer.Column));
    }

    [Fact]
    public void Backspace_AtLineStart_MergesWithPreviousLine()
    {
        var buffer = new TextBuffer(["ab", "cd"]);
        buffer.MoveDown();

        buffer.Backspace();

        Assert.Equal("abcd", string.Join('|', buffer.Lines));
        Assert.Equal((0, 2), (buffer.Row, buffer.Column));
    }

    [Fact]
    public void Backspace_AtTheVeryStart_DoesNothing()
    {
        var buffer = new TextBuffer(["ab"]);

        buffer.Backspace();

        Assert.Equal("ab", string.Join('|', buffer.Lines));
        Assert.False(buffer.IsDirty);
    }

    [Fact]
    public void Delete_AtLineEnd_MergesWithNextLine()
    {
        var buffer = new TextBuffer(["ab", "cd"]);
        buffer.MoveEnd();

        buffer.Delete();

        Assert.Equal("abcd", string.Join('|', buffer.Lines));
        Assert.Equal(2, buffer.Column);
    }

    [Fact]
    public void VerticalMovement_RemembersTheDesiredColumn()
    {
        var buffer = new TextBuffer(["long line", "ab", "another long"]);
        buffer.MoveEnd();

        buffer.MoveDown();
        Assert.Equal(2, buffer.Column);

        buffer.MoveDown();
        Assert.Equal(9, buffer.Column);
    }

    [Fact]
    public void HorizontalMovement_WrapsBetweenLines()
    {
        var buffer = new TextBuffer(["a", "b"]);
        buffer.MoveRight();

        buffer.MoveRight();
        Assert.Equal((1, 0), (buffer.Row, buffer.Column));

        buffer.MoveLeft();
        Assert.Equal((0, 1), (buffer.Row, buffer.Column));
    }

    [Fact]
    public void MarkSaved_ClearsTheDirtyFlag_MovementNeverSetsIt()
    {
        var buffer = new TextBuffer(["a"]);
        buffer.MoveRight();
        Assert.False(buffer.IsDirty);

        buffer.Insert('x');
        Assert.True(buffer.IsDirty);

        buffer.MarkSaved();
        Assert.False(buffer.IsDirty);
    }
}
