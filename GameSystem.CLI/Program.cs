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
IGameStateRepository states = new JsonGameStateRepository(
    Path.Combine(Directory.GetCurrentDirectory(), "saves"));
ICharacterProfileSource profiles = new JsonCharacterProfileSource(
    Path.Combine(Directory.GetCurrentDirectory(), "characters"));
IGameResultRepository results = new JsonGameResultRepository(
    Path.Combine(Directory.GetCurrentDirectory(), "results", "game-results.json"));

var httpClient = new HttpClient { BaseAddress = new Uri("https://genshin.jmp.blue/") };
IGenshinApiClient apiClient = new HttpGenshinApiClient(httpClient);

// The facade, already plugged with the information, transformative and infrastructure strategies.
Cli cli = CliFactory.Create(displayer, input, logger, states, results, apiClient, profiles: profiles);

displayer.Show("--- GameSystem CLI Started. Type 'help' for commands, 'exit' to quit. ---", DisplayKind.Title);

while (true)
{
    string? line = input.ReadLine("> ");
    if (line is null) break;
    if (string.IsNullOrWhiteSpace(line)) continue;
    if (line.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    cli.ExecCommand(line);
}
