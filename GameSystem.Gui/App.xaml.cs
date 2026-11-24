using System.IO;
using System.Net.Http;
using System.Windows;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Engine;
using GameSystem.Infra.Persistence;
using GameSystem.Infra.WebApi;
using GameSystem.Presentation;
using GameSystem.Presentation.Console;

namespace GameSystem.Gui;

/// <summary>
/// The composition root of the windowed program: the only place that knows the concrete classes.
/// It plugs the same Infra implementations as the console program into the game and replaces the console by the window.
/// </summary>
public partial class App : Application
{
    private GameHost? _host;
    private MainViewModel? _viewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string dataRoot = AppDataLocations.Root();

        var console = new GuiConsole();
        IGameStateRepository states = new JsonGameStateRepository(AppDataLocations.Saves(dataRoot));
        IGameResultRepository results = new JsonGameResultRepository(AppDataLocations.Results(dataRoot));
        ICharacterProfileSource profiles = new JsonCharacterProfileSource(Path.Combine(AppContext.BaseDirectory, "characters"));

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://genshin.jmp.blue/"),
            Timeout = TimeSpan.FromSeconds(15)
        };
        IGenshinApiClient apiClient = new HttpGenshinApiClient(httpClient);
        IPortraitProvider portraits = new HttpPortraitProvider(httpClient, AppDataLocations.Portraits(dataRoot));
        ICharacterCatalogStore catalogStore = new JsonCharacterCatalogStore(AppDataLocations.Catalog(dataRoot));

        _host = new GameHost(
            console,
            states,
            results,
            apiClient,
            profiles,
            welcomeNote: $"Saves and results are kept in: {dataRoot}",
            catalogStore: catalogStore,
            portraits: portraits);

        // The view model is created on the UI thread, so it captures the dispatcher's synchronization context.
        _viewModel = new MainViewModel(_host, console, portraits);

        var window = new MainWindow { DataContext = _viewModel };
        window.Show();

        _host.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _viewModel?.Dispose();
        _host?.Dispose();
        base.OnExit(e);
    }
}
