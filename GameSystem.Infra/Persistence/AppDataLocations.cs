namespace GameSystem.Infra.Persistence;

/// <summary>
/// Where the game keeps its files. They go to the per-user application data folder instead of the folder the
/// program was started from: <c>%LOCALAPPDATA%\GameSystem</c> on Windows, <c>~/.local/share/GameSystem</c> on Linux,
/// <c>~/Library/Application Support/GameSystem</c> on macOS. The environment variable <c>GAMESYSTEM_HOME</c>
/// replaces the whole folder (useful for portable runs and for tests).
/// </summary>
public static class AppDataLocations
{
    public const string HomeVariable = "GAMESYSTEM_HOME";

    public const string ApplicationFolder = "GameSystem";

    /// <summary>Pure decision, so it can be tested without touching the real environment.</summary>
    public static string Resolve(string? homeOverride, string? localAppData)
    {
        if (!string.IsNullOrWhiteSpace(homeOverride))
            return homeOverride;

        // Some minimal environments have no application data folder at all: stay next to the program then.
        if (string.IsNullOrWhiteSpace(localAppData))
            return Path.Combine(AppContext.BaseDirectory, "data");

        return Path.Combine(localAppData, ApplicationFolder);
    }

    public static string Root() => Resolve(
        Environment.GetEnvironmentVariable(HomeVariable),
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    public static string Saves(string root) => Path.Combine(root, "saves");

    public static string Portraits(string root) => Path.Combine(root, "portraits");

    public static string Results(string root) => Path.Combine(root, "results", "game-results.json");
}
