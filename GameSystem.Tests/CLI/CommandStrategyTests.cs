using GameSystem.CLI;
using GameSystem.CLI.Strategies;
using Xunit;

namespace GameSystem.Tests.CLI;

public class CommandStrategyTests
{
    private sealed class DemoStrategy : CommandStrategyBase
    {
        public DemoStrategy() : base("Demo")
        {
        }
    }

    [Fact]
    public void RegisterCommand_MakesTheSelectorAvailable_IgnoringCase()
    {
        var strategy = new DemoStrategy();

        strategy.RegisterCommand(new RelayCommand("Hello", "says hello", _ => { }));

        Assert.Contains("hello", strategy.GetCommandSelectors(), StringComparer.OrdinalIgnoreCase);
        Assert.Single(strategy.GetCommands());
    }

    [Fact]
    public void RegisterCommand_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DemoStrategy().RegisterCommand(null!));
    }

    [Fact]
    public void ExecuteCommand_RunsTheCommandWithTheGivenArguments()
    {
        var strategy = new DemoStrategy();
        CommandContext? received = null;
        strategy.RegisterCommand(new RelayCommand("run", "", ctx => received = ctx));
        var args = new CommandContext("run", ["x"]);

        strategy.ExecuteCommand("RUN", args);

        Assert.Same(args, received);
    }

    [Fact]
    public void ExecuteCommand_UnknownSelector_Throws()
    {
        var strategy = new DemoStrategy();

        Assert.Throws<KeyNotFoundException>(() => strategy.ExecuteCommand("nope", new CommandContext("nope")));
    }
}
