using GameSystem.CLI;
using GameSystem.Core.Abstractions;
using GameSystem.FileManager.App;
using GameSystem.FileManager.Infrastructure;
using GameSystem.Infra.Display;

// ---- Composition root: the only place that knows the concrete implementations ----
IDisplayer displayer = new ConsoleDisplayer();

var startDirectory = args.Length > 0 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();

if (!Directory.Exists(startDirectory))
{
    displayer.Error($"Directory not found: {startDirectory}");
    return 1;
}

if (Console.IsInputRedirected || Console.IsOutputRedirected)
{
    displayer.Error("The file manager needs an interactive terminal (input and output must not be redirected).");
    return 1;
}

var app = new FileManagerApp(new ConsoleTerminal(), new PhysicalFileSystem(), new CommandParser(), startDirectory);
app.Run();

displayer.Show("Goodbye!", DisplayKind.Title);
return 0;
