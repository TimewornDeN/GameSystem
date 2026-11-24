using GameSystem.Presentation;
using GameSystem.Presentation.Console;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.Gui;

/// <summary>The game loop behind the window: the same commands as the console, driven through the window's console.</summary>
public class GameHostTests
{
    [Fact]
    public void TheGameStartsAtTheMainPrompt_WithAnEmptyCatalog()
    {
        using var game = new HostHarness();

        var first = game.WaitFor(_ => true);

        Assert.Equal(PromptMode.Main, first.Mode);
        Assert.Equal(GameHost.MainPrompt, first.Prompt);
        Assert.Empty(first.Cards);
        Assert.Contains(game.Lines, l => l.Style == LineStyle.Title && l.Text.Contains("GameSystem"));
    }

    [Fact]
    public void CreatingACharacter_ThroughItsDialog_ShowsUpAsACard()
    {
        using var game = new HostHarness();
        game.WaitFor(s => s.Mode == PromptMode.Main);

        game.Type("create char");
        var asking = game.WaitFor(s => s.Mode == PromptMode.Dialog);
        Assert.Contains("name", asking.Prompt, StringComparison.OrdinalIgnoreCase);

        game.Type("Grosh");
        game.WaitFor(s => s.Mode == PromptMode.Dialog);
        game.Type("1");

        var done = game.WaitFor(s => s.Mode == PromptMode.Main && s.Cards.Count == 1);
        Assert.Equal("Grosh", done.Cards[0].Name);
        Assert.Equal("warrior", done.Cards[0].ClassId);
    }

    [Fact]
    public void ASavedGame_AppearsInTheListOfSaves()
    {
        using var game = new HostHarness();
        game.WaitFor(s => s.Mode == PromptMode.Main);

        game.Type("save evening");

        var snapshot = game.WaitFor(s => s.Saves.Contains("evening"));
        Assert.Equal("evening", snapshot.LatestSave);
        Assert.True(game.States.Exists("evening"));
    }

    [Fact]
    public void ABattle_SwitchesToBattleMode_AndReturnsToTheMainPromptWhenItEnds()
    {
        using var game = new HostHarness();
        game.WaitFor(s => s.Mode == PromptMode.Main);
        game.Type("create char", "Grosh", "1", "create char", "Elara", "2");
        game.WaitFor(s => s.Mode == PromptMode.Main && s.Cards.Count == 2);

        game.Type("play Grosh Elara");
        var fighting = game.WaitFor(s => s.Mode == PromptMode.Battle);

        Assert.StartsWith("[Grosh]", fighting.Prompt);
        Assert.Contains("Grosh", fighting.BattleSummary);
        Assert.All(fighting.Cards, c => Assert.True(c.IsFighting));

        game.Type("surrender");
        var finished = game.WaitFor(s => s.Mode == PromptMode.Main && game.Results.Items.Count == 1);

        Assert.Equal(string.Empty, finished.BattleSummary);
        Assert.Equal("Elara", game.Results.Items[0].Winner);
    }

    [Fact]
    public void ABattleSavedInTheWindow_CanBeContinuedByAnotherWindow()
    {
        var states = new InMemoryGameStateRepository();
        var results = new InMemoryGameResultRepository();

        using (var first = new HostHarness(states, results))
        {
            first.WaitFor(s => s.Mode == PromptMode.Main);
            first.Type("create char", "Grosh", "1", "create char", "Elara", "2");
            first.WaitFor(s => s.Cards.Count == 2);
            first.Type("play Grosh Elara");
            first.WaitFor(s => s.Mode == PromptMode.Battle);
            first.Type("attack");
            first.WaitFor(s => s.Mode == PromptMode.Battle && s.Prompt.StartsWith("[Elara]"));
            first.Type("save mid");
            first.WaitFor(s => s.Saves.Contains("mid"));
        }

        using var second = new HostHarness(states, results);
        second.WaitFor(s => s.Mode == PromptMode.Main);
        second.Type("continue");

        var resumed = second.WaitFor(s => s.Mode == PromptMode.Battle);

        Assert.StartsWith("[Elara]", resumed.Prompt);
        Assert.Equal(2, resumed.Cards.Count);
        Assert.Contains("Elara", resumed.BattleSummary);
    }

    [Fact]
    public void Exit_DoesNotStopTheGame_ItAsksToCloseTheWindow()
    {
        using var game = new HostHarness();
        game.WaitFor(s => s.Mode == PromptMode.Main);

        game.Type("exit");
        game.WaitFor(s => s.Mode == PromptMode.Main && game.Lines.Any(l => l.Text.Contains("Close the window")));

        Assert.False(game.Console.IsClosed);
    }

    [Fact]
    public void ClosingTheInput_EndsTheGameLoop()
    {
        var game = new HostHarness();
        game.WaitFor(s => s.Mode == PromptMode.Main);

        game.Console.Close();

        Assert.True(game.Host.WaitForExit(TimeSpan.FromSeconds(5)));
        game.Dispose();
    }

    [Fact]
    public void TheSavedServerCatalog_IsPartOfTheSnapshots_AndIsReadAgainAfterTheCatalogCommand()
    {
        var store = new InMemoryCharacterCatalogStore();
        store.Items.Add(new GameSystem.Core.Models.RemoteCharacterInfo("zed", "Zed", "", "Electro", "Catalyst", 4, "", []));
        var api = new FakeGenshinApiClient();
        api.AddCharacter(new GameSystem.Core.Models.RemoteCharacterInfo("albedo", "Albedo", "Kreideprinz", "Geo", "Sword", 5, "", []));
        using var game = new HostHarness(api: api, catalogStore: store);

        var first = game.WaitFor(s => s.Mode == PromptMode.Main);
        Assert.Equal(["Zed"], first.ServerCatalog!.Select(c => c.Name));

        game.Type("catalog refresh");

        var after = game.WaitFor(s => s.Mode == PromptMode.Main && s.ServerCatalog is { Count: 1 } list && list[0].Name == "Albedo");
        Assert.Equal("Kreideprinz", after.ServerCatalog![0].Title);
    }
}
