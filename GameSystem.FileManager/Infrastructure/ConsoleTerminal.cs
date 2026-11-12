using System.Runtime.InteropServices;
using System.Text;
using GameSystem.FileManager.Rendering;
using GameSystem.FileManager.Terminal;

namespace GameSystem.FileManager.Infrastructure;

/// <summary>The real console. This is the only class that talks to <see cref="Console"/>.</summary>
public sealed class ConsoleTerminal : ITerminal
{
    private const int StdOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    public int Width => Console.WindowWidth;

    public int Height => Console.WindowHeight;

    public void Open()
    {
        Console.OutputEncoding = Encoding.UTF8;
        EnableAnsiOnWindows();

        // Ctrl+C arrives as a normal key, so the terminal is always restored by Close().
        Console.TreatControlCAsInput = true;
        Console.Write(Ansi.EnterAlternateScreen + Ansi.ClearScreen);
    }

    public void Close()
    {
        Console.TreatControlCAsInput = false;
        Console.Write(Ansi.Reset + Ansi.ShowCursor + Ansi.ExitAlternateScreen);
    }

    public void Present(string frame) => Console.Write(frame);

    public ConsoleKeyInfo ReadKey() => Console.ReadKey(intercept: true);

    private static void EnableAnsiOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var handle = GetStdHandle(StdOutputHandle);
        if (GetConsoleMode(handle, out var mode))
        {
            SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
        }
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetStdHandle(int handle);

    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr handle, out uint mode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr handle, uint mode);
}
