using GameSystem.Core.Abstractions;
using GameSystem.Core.Logging;
using GameSystem.Core.Models;
using GameSystem.Presentation.Console;
using Xunit;

namespace GameSystem.Tests.Gui;

public class GuiConsoleTests
{
    private static List<ConsoleLine> Collect(GuiConsole console)
    {
        List<ConsoleLine> lines = [];
        console.LineWritten += lines.Add;
        return lines;
    }

    [Fact]
    public void Display_WritesTheTextWithTheSamePrefixAsTheConsole()
    {
        var console = new GuiConsole();
        var lines = Collect(console);

        console.Display(new Message("Saved.", DisplayKind.Success));
        console.Display(new Message("Boom", DisplayKind.Error));
        console.Display(new Message("Plain text"));

        Assert.Equal(
            [new ConsoleLine("[Success]: Saved.", LineStyle.Success), new ConsoleLine("[Error]: Boom", LineStyle.Error), new ConsoleLine("Plain text", LineStyle.Plain)],
            lines);
    }

    [Theory]
    [InlineData(LogColor.Red, LineStyle.Damage)]
    [InlineData(LogColor.Green, LineStyle.Heal)]
    [InlineData(LogColor.Magenta, LineStyle.Magic)]
    [InlineData(LogColor.Default, LineStyle.Plain)]
    public void Log_MapsTheColourHintToALineStyle(LogColor color, LineStyle expected)
    {
        var console = new GuiConsole();
        var lines = Collect(console);

        console.Log("hit", color);

        Assert.Equal(new ConsoleLine("hit", expected), Assert.Single(lines));
    }

    [Fact]
    public async Task ReadLine_ReturnsWhatTheWindowSubmits_AndEchoesItWithThePrompt()
    {
        var console = new GuiConsole();
        var lines = Collect(console);

        using var waiting = new ManualResetEventSlim();
        console.WaitingForInput += _ => waiting.Set();

        var reading = Task.Run(() => console.ReadLine("[Grosh] > "));
        Assert.True(waiting.Wait(TimeSpan.FromSeconds(5)));
        console.Submit("attack");

        Assert.Equal("attack", await reading.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Contains(new ConsoleLine("[Grosh] > attack", LineStyle.Echo), lines);
    }

    [Fact]
    public void ReadLine_RaisesWaitingForInputWithThePrompt()
    {
        var console = new GuiConsole();
        string? seen = null;
        console.WaitingForInput += p => seen = p;
        console.Submit("go");

        console.ReadLine("Name: ");

        Assert.Equal("Name: ", seen);
    }

    [Fact]
    public async Task Close_EndsAWaitingReadLine_WithNull()
    {
        var console = new GuiConsole();

        var reading = Task.Run(() => console.ReadLine());
        await Task.Delay(50);
        console.Close();

        Assert.Null(await reading.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(console.IsClosed);
    }

    [Fact]
    public void Submit_AfterClose_IsIgnored()
    {
        var console = new GuiConsole();
        var lines = Collect(console);
        console.Close();

        console.Submit("too late");

        Assert.Empty(lines);
    }
}
