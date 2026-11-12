using GameSystem.CLI;
using GameSystem.CLI.Interfaces;
using GameSystem.CLI.Strategies;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Rendering;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.CLI;

public class CliFacadeTests
{
    private sealed class TestStrategy : CommandStrategyBase
    {
        public TestStrategy(string name, params ICommand[] commands) : base(name)
        {
            foreach (var command in commands) RegisterCommand(command);
        }
    }

    private sealed class RecordingRenderer<T> : IRenderer<T>
    {
        public List<T> Rendered { get; } = [];

        public void Render(T data) => Rendered.Add(data);
    }

    private static (Cli Cli, FakeDisplayer Displayer) CreateCli()
    {
        var displayer = new FakeDisplayer();
        return (new Cli(new CommandParser(), displayer), displayer);
    }

    [Fact]
    public void ExecCommand_RoutesToTheStrategyThatOwnsTheCommand()
    {
        var (cli, _) = CreateCli();
        CommandContext? received = null;
        var calledA = false;

        cli.UseStrategy(new TestStrategy("First", new RelayCommand("a", "", _ => calledA = true)));
        cli.UseStrategy(new TestStrategy("Second", new RelayCommand("b", "", ctx => received = ctx)));

        var found = cli.ExecCommand("b one --flag");

        Assert.True(found);
        Assert.False(calledA);
        Assert.NotNull(received);
        Assert.Equal("one", Assert.Single(received!.Args));
        Assert.True(received.HasOption("flag"));
    }

    [Fact]
    public void ExecCommand_CommandNamesAreCaseInsensitive()
    {
        var (cli, _) = CreateCli();
        var called = false;
        cli.UseStrategy(new TestStrategy("Only", new RelayCommand("Ping", "", _ => called = true)));

        cli.ExecCommand("PING");

        Assert.True(called);
    }

    [Fact]
    public void ExecCommand_UnknownCommand_WarnsAndReturnsFalse()
    {
        var (cli, displayer) = CreateCli();

        var found = cli.ExecCommand("dance");

        Assert.False(found);
        Assert.Contains(displayer.Entries, e => e.Kind == DisplayKind.Warning && e.Text.Contains("Unknown command 'dance'"));
    }

    [Fact]
    public void ExecCommand_BlankInput_DoesNothing()
    {
        var (cli, displayer) = CreateCli();

        Assert.True(cli.ExecCommand("   "));
        Assert.Empty(displayer.Entries);
    }

    [Fact]
    public void ExecCommand_ThrowingCommand_ReportsExecutionErrorInsteadOfCrashing()
    {
        var (cli, displayer) = CreateCli();
        cli.UseStrategy(new TestStrategy("Bad", new RelayCommand("boom", "", _ => throw new InvalidOperationException("kaboom"))));

        cli.ExecCommand("boom");

        Assert.True(displayer.HasError("Execution Error: kaboom"));
    }

    [Fact]
    public void UseStrategy_TwoStrategiesWithTheSameCommand_Throws()
    {
        var (cli, _) = CreateCli();
        cli.UseStrategy(new TestStrategy("First", new RelayCommand("same", "", _ => { })));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            cli.UseStrategy(new TestStrategy("Second", new RelayCommand("SAME", "", _ => { }))));

        Assert.Contains("First", ex.Message);
    }

    [Fact]
    public void Strategies_AreExposedInTheOrderTheyWereAdded()
    {
        var (cli, _) = CreateCli();
        cli.UseStrategy(new TestStrategy("One"));
        cli.UseStrategy(new TestStrategy("Two"));

        Assert.Equal("One,Two", string.Join(',', cli.Strategies.Select(s => s.Name)));
    }

    [Fact]
    public void Display_UsesTheRendererRegisteredForTheDataType()
    {
        var (cli, _) = CreateCli();
        var renderer = new RecordingRenderer<string>();
        cli.UseRenderer(renderer);

        cli.Display(new Renderable<string>("hello"));

        Assert.Equal("hello", Assert.Single(renderer.Rendered));
    }

    [Fact]
    public void Display_WithoutRegisteredRenderer_UsesTheRendererOfTheObject()
    {
        var (cli, _) = CreateCli();
        var own = new RecordingRenderer<int>();
        var renderable = new Renderable<int>(42);
        renderable.UseRenderer(own);

        cli.Display(renderable);

        Assert.Equal(42, Assert.Single(own.Rendered));
    }

    [Fact]
    public void Display_WithoutAnyRenderer_Throws()
    {
        var (cli, _) = CreateCli();

        Assert.Throws<InvalidOperationException>(() => cli.Display(new Renderable<double>(1.5)));
    }

    [Fact]
    public void Help_ListsEveryCategoryAndItsCommands()
    {
        var displayer = new FakeDisplayer();
        var cli = CliFactory.Create(
            displayer, new FakeInputReader(), new FakeCombatLogger(),
            new InMemoryGameStateRepository(), new InMemoryGameResultRepository());

        cli.ExecCommand("help");

        Assert.True(displayer.Any("[Information]"));
        Assert.True(displayer.Any("[Transformative]"));
        Assert.True(displayer.Any("[Infrastructure]"));
        foreach (var command in new[] { "ls", "sheet", "results", "create", "add", "act", "play", "save", "load", "list", "remote" })
            Assert.Contains(displayer.Entries, e => e.Text.TrimStart().StartsWith(command + " "));
    }
}
