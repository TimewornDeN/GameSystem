using GameSystem.CLI;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Logging;
using GameSystem.Infra.Display;
using GameSystem.Infra.Input;
using GameSystem.Infra.Logging;
using GameSystem.Infra.Persistence;
using GameSystem.Infra.WebApi;

// ---- Composition root: the only place that knows the concrete implementations ----
IDisplayer displayer = new ConsoleDisplayer();
IInputReader input = new ConsoleInputReader();
ICombatLogger logger = new ConsoleCombatLogger();
// Saves and results live in the per-user application data folder, not in whatever folder the program was started from.
string dataRoot = AppDataLocations.Root();
IGameStateRepository states = new JsonGameStateRepository(AppDataLocations.Saves(dataRoot));
ICharacterProfileSource profiles = new JsonCharacterProfileSource(
    Path.Combine(AppContext.BaseDirectory, "characters"));
IGameResultRepository results = new JsonGameResultRepository(AppDataLocations.Results(dataRoot));

var httpClient = new HttpClient { BaseAddress = new Uri("https://genshin.jmp.blue/") };
IGenshinApiClient apiClient = new HttpGenshinApiClient(httpClient);

// The facade, already plugged with the information, transformative and infrastructure strategies.
Cli cli = CliFactory.Create(displayer, input, logger, states, results, apiClient, profiles: profiles);

displayer.Show("--- GameSystem CLI Started. Type 'help' for commands, 'exit' to quit. ---", DisplayKind.Title);
displayer.Info($"Saves and results are kept in: {dataRoot}");
if (states.Latest() is { } lastSave)
    displayer.Info($"Last save: '{lastSave}'. Type 'continue' to go on from it.");

while (true)
{
    string? line = input.ReadLine("> ");
    if (line is null) break;
    if (string.IsNullOrWhiteSpace(line)) continue;
    if (line.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    cli.ExecCommand(line);
}
