using GameSystem.CLI;
using Xunit;

namespace GameSystem.Tests.CLI;

public class CommandParserTests
{
    private readonly CommandParser _parser = new();

    [Fact]
    public void Parse_EmptyInput_ReturnsEmptyCommandName()
    {
        var ctx = _parser.Parse("   ");

        Assert.Equal(string.Empty, ctx.CommandName);
    }

    [Fact]
    public void Parse_LowercasesCommandName()
    {
        var ctx = _parser.Parse("CREATE char");

        Assert.Equal("create", ctx.CommandName);
    }

    [Fact]
    public void Parse_SplitsPositionalArguments()
    {
        var ctx = _parser.Parse("act attack Grosh Elara");

        Assert.Equal(["attack", "Grosh", "Elara"], ctx.Args);
    }

    [Fact]
    public void Parse_HandlesQuotedArgumentsAsOneToken()
    {
        var ctx = _parser.Parse("add --char_id Grosh --id \"Flaming Sword\"");

        Assert.Equal("Flaming Sword", ctx.GetOption("id"));
    }

    [Fact]
    public void Parse_OptionWithValue_IsCaptured()
    {
        var ctx = _parser.Parse("act ability Grosh Elara --id shield-bash");

        Assert.True(ctx.HasOption("id"));
        Assert.Equal("shield-bash", ctx.GetOption("id"));
        Assert.Equal(["ability", "Grosh", "Elara"], ctx.Args);
    }

    [Fact]
    public void Parse_FlagWithoutValue_DefaultsToTrueString()
    {
        var ctx = _parser.Parse("ls char --whole");

        Assert.Equal("true", ctx.GetOption("whole"));
    }

    [Fact]
    public void Parse_UnknownOption_ReturnsNull()
    {
        var ctx = _parser.Parse("ls char");

        Assert.Null(ctx.GetOption("id"));
        Assert.False(ctx.HasOption("id"));
    }
}
