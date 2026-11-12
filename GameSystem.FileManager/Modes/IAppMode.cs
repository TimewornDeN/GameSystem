using GameSystem.FileManager.Rendering;

namespace GameSystem.FileManager.Modes;

/// <summary>One working mode of the application (State pattern: the state interface).</summary>
public interface IAppMode
{
    string Name { get; }

    /// <summary>Fills the canvas segments for the current state of the mode.</summary>
    void Render(Canvas canvas);

    /// <summary>Interprets a key press. The same key may mean different things in different modes.</summary>
    void HandleKey(ConsoleKeyInfo key);
}
