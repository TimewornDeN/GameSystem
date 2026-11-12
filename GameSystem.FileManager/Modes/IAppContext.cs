using GameSystem.CLI.Interfaces;
using GameSystem.FileManager.Files;

namespace GameSystem.FileManager.Modes;

/// <summary>What a mode is allowed to ask from the application that owns it (State pattern: the context).</summary>
public interface IAppContext
{
    IFileSystem FileSystem { get; }

    /// <summary>The command parser of the CLI module, shared by the command bars of all modes.</summary>
    ICommandParser CommandParser { get; }

    /// <summary>Number of body rows currently available; used for paging.</summary>
    int BodyHeight { get; }

    /// <summary>Switches the application to another mode.</summary>
    void ChangeMode(IAppMode mode);

    /// <summary>Ends the application loop.</summary>
    void Exit();
}
