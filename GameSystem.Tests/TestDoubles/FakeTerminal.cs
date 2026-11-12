using GameSystem.FileManager.Terminal;

namespace GameSystem.Tests.TestDoubles;

/// <summary>A scripted terminal: keys come from a queue, frames are remembered instead of printed.</summary>
internal sealed class FakeTerminal : ITerminal
{
    private readonly Queue<ConsoleKeyInfo> _keys = new();

    public int Width { get; set; } = 60;

    public int Height { get; set; } = 14;

    public string LastFrame { get; private set; } = string.Empty;

    public bool IsOpen { get; private set; }

    public int OpenCount { get; private set; }

    public void Enqueue(params ConsoleKeyInfo[] keys)
    {
        foreach (var key in keys)
        {
            _keys.Enqueue(key);
        }
    }

    public void Open()
    {
        IsOpen = true;
        OpenCount++;
    }

    public void Close() => IsOpen = false;

    public void Present(string frame) => LastFrame = frame;

    public ConsoleKeyInfo ReadKey() => _keys.Dequeue();
}
