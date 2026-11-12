namespace GameSystem.FileManager.Terminal;

/// <summary>
/// The only place where the application touches the real console.
/// Everything else depends on this interface, so tests can replace it with a fake.
/// </summary>
public interface ITerminal
{
    int Width { get; }

    int Height { get; }

    /// <summary>Prepares the terminal (alternate screen, ANSI support).</summary>
    void Open();

    /// <summary>Restores the terminal to its original state.</summary>
    void Close();

    /// <summary>Draws a complete frame produced by <see cref="Rendering.Canvas"/>.</summary>
    void Present(string frame);

    /// <summary>Blocks until the user presses a key.</summary>
    ConsoleKeyInfo ReadKey();
}
