using GameSystem.Core.Models;
using GameSystem.Presentation;
using GameSystem.Presentation.Console;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.Gui;

/// <summary>The view model of the window, driven the way the buttons and the input line drive it.</summary>
public class MainViewModelTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);

    private static MainViewModel CreateViewModel(HostHarness game)
    {
        // The posting function runs the action on the calling thread: no window, so no dispatcher.
        var viewModel = new MainViewModel(game.Host, game.Console, portraits: null, post: action => action());
        game.Host.Start();
        return viewModel;
    }

    private static void WaitUntil(Func<bool> condition)
        => Assert.True(SpinWait.SpinUntil(condition, Limit), "The window did not reach the expected state in time.");

    [Fact]
    public void AfterStart_TheWindowIsAtTheMainPrompt_AndShowsTheWelcomeText()
    {
        using var game = new HostHarness(start: false);
        using var viewModel = CreateViewModel(game);

        WaitUntil(() => viewModel.Mode == PromptMode.Main);

        Assert.True(viewModel.CanSend);
        Assert.True(viewModel.IsMain);
        Assert.False(viewModel.IsBattle);
        Assert.Equal(GameHost.MainPrompt, viewModel.Prompt);
        Assert.Contains(viewModel.Lines, l => l.Style == LineStyle.Title);
    }

    [Fact]
    public void SaveButton_NeedsANameAndAMainOrBattlePrompt()
    {
        using var game = new HostHarness(start: false);
        using var viewModel = CreateViewModel(game);
        WaitUntil(() => viewModel.Mode == PromptMode.Main);

        Assert.False(viewModel.CanSave);

        viewModel.SaveName = "evening";

        Assert.True(viewModel.CanSave);
    }

    [Fact]
    public void SaveButton_SavesTheGame_AndTheNewSaveAppearsInTheList()
    {
        using var game = new HostHarness(start: false);
        using var viewModel = CreateViewModel(game);
        WaitUntil(() => viewModel.Mode == PromptMode.Main);

        viewModel.SaveName = "my save";
        viewModel.SaveGameCommand.Execute(null);

        WaitUntil(() => viewModel.Saves.Contains("my_save"));
        Assert.True(game.States.Exists("my_save"));
    }

    [Fact]
    public void ChoosingASave_FillsTheNameBox_AndEnablesLoad()
    {
        using var game = new HostHarness(start: false);
        using var viewModel = CreateViewModel(game);
        WaitUntil(() => viewModel.Mode == PromptMode.Main);
        viewModel.SaveName = "slot";
        viewModel.SaveGameCommand.Execute(null);
        WaitUntil(() => viewModel.Saves.Contains("slot") && viewModel.Mode == PromptMode.Main);
        viewModel.SaveName = string.Empty;

        viewModel.SelectedSave = "slot";

        Assert.Equal("slot", viewModel.SaveName);
        Assert.True(viewModel.CanLoad);
        Assert.True(viewModel.CanContinue);
    }

    [Fact]
    public void LoadButton_RestoresTheWorld_AndTheCatalogFollows()
    {
        var states = new InMemoryGameStateRepository();
        using (var earlier = new HostHarness(states))
        {
            earlier.WaitFor(s => s.Mode == PromptMode.Main);
            earlier.Type("create char", "Grosh", "1", "save old");
            earlier.WaitFor(s => s.Saves.Contains("old"));
        }

        using var game = new HostHarness(states, start: false);
        using var viewModel = CreateViewModel(game);
        WaitUntil(() => viewModel.Mode == PromptMode.Main && viewModel.Saves.Contains("old"));
        Assert.True(viewModel.Catalog.IsEmpty);

        viewModel.SelectedSave = "old";
        viewModel.LoadGameCommand.Execute(null);

        WaitUntil(() => viewModel.Catalog.Cards.Count == 1 && viewModel.Mode == PromptMode.Main);
        Assert.Equal("Grosh", viewModel.Catalog.Cards[0].Card.Name);
    }

    [Fact]
    public void TheInputLine_AnswersADialog()
    {
        using var game = new HostHarness(start: false);
        using var viewModel = CreateViewModel(game);
        WaitUntil(() => viewModel.Mode == PromptMode.Main);

        viewModel.QuickCommand.Execute("create char");
        WaitUntil(() => viewModel.Mode == PromptMode.Dialog);
        Assert.False(viewModel.IsMain); // quick buttons of the main prompt are off while a question is open

        viewModel.InputText = "Grosh";
        viewModel.SendCommand.Execute(null);
        Assert.Equal(string.Empty, viewModel.InputText);
        WaitUntil(() => viewModel.Mode == PromptMode.Dialog);

        viewModel.InputText = "1";
        viewModel.SendCommand.Execute(null);

        WaitUntil(() => viewModel.Mode == PromptMode.Main && viewModel.Catalog.Cards.Count == 1);
    }

    [Fact]
    public void AddFromServer_CreatesACharacterWithTheServerData()
    {
        var api = new FakeGenshinApiClient();
        api.AddCharacter(new RemoteCharacterInfo("albedo", "Albedo", "Kreideprinz", "Geo", "Sword", 5, "Chief Alchemist.", []));
        using var game = new HostHarness(start: false, api: api);
        using var viewModel = CreateViewModel(game);
        WaitUntil(() => viewModel.Mode == PromptMode.Main);

        Assert.False(viewModel.CanAddRemote);
        viewModel.RemoteId = "albedo";
        viewModel.RemoteName = "My Albedo";
        Assert.True(viewModel.CanAddRemote);
        viewModel.AddRemoteCommand.Execute(null);

        WaitUntil(() => viewModel.Catalog.Cards.Count == 1);
        var card = viewModel.Catalog.Cards[0].Card;
        Assert.Equal("My_Albedo", card.Name);
        Assert.Equal("albedo", card.RemoteId);
        Assert.Equal("Kreideprinz", card.Title);
        Assert.Equal(5, card.Rarity);
    }

    [Fact]
    public void ABattle_EnablesTheBattleButtons_AndShowsTheSummary()
    {
        using var game = new HostHarness(start: false);
        using var viewModel = CreateViewModel(game);
        WaitUntil(() => viewModel.Mode == PromptMode.Main);
        game.Type("create char", "Grosh", "1", "create char", "Elara", "2");
        WaitUntil(() => viewModel.Catalog.Cards.Count == 2 && viewModel.Mode == PromptMode.Main);

        viewModel.QuickCommand.Execute("play Grosh Elara");

        WaitUntil(() => viewModel.IsBattle);
        Assert.True(viewModel.HasBattle);
        Assert.Contains("Grosh", viewModel.BattleSummary);
        Assert.False(viewModel.CanLoad);
        Assert.False(viewModel.CanContinue);
        Assert.True(viewModel.Catalog.Cards.All(c => c.Card.IsFighting));
    }

    [Fact]
    public void TheAddButtonOfAServerCard_CreatesThatCharacterInTheGame_AndShowsTheResult()
    {
        var api = new FakeGenshinApiClient();
        api.AddCharacter(new RemoteCharacterInfo("albedo", "Albedo", "Kreideprinz", "Geo", "Sword", 5, "Chief Alchemist.", []));
        using var game = new HostHarness(start: false, api: api);
        using var viewModel = CreateViewModel(game);
        WaitUntil(() => viewModel.Mode == PromptMode.Main);

        viewModel.AddServerCharacterCommand.Execute("albedo");

        WaitUntil(() => viewModel.Catalog.Cards.Count == 1 && viewModel.Mode == PromptMode.Main);
        Assert.Equal("albedo", viewModel.Catalog.Cards[0].Card.RemoteId);
        Assert.Contains("Created", viewModel.StatusText);
    }

    [Fact]
    public void TheRefreshButtonOfTheCatalog_FillsTheServerCards()
    {
        var api = new FakeGenshinApiClient();
        api.AddCharacter(new RemoteCharacterInfo("albedo", "Albedo", "Kreideprinz", "Geo", "Sword", 5, "Chief Alchemist.", []));
        api.AddCharacter(new RemoteCharacterInfo("amber", "Amber", "Outrider", "Pyro", "Bow", 4, "A scout.", []));
        using var game = new HostHarness(start: false, api: api, catalogStore: new InMemoryCharacterCatalogStore());
        using var viewModel = CreateViewModel(game);
        WaitUntil(() => viewModel.Mode == PromptMode.Main);
        Assert.True(viewModel.Catalog.ServerIsEmpty);

        viewModel.QuickCommand.Execute("catalog refresh");

        WaitUntil(() => viewModel.Catalog.ServerCards.Count == 2 && viewModel.Mode == PromptMode.Main);
        Assert.Equal(["Albedo", "Amber"], viewModel.Catalog.ServerCards.Select(c => c.Card.Name));
        Assert.False(viewModel.IsBusy);
    }
}
