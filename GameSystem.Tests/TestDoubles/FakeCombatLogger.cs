using GameSystem.Core.Logging;

namespace GameSystem.Tests.TestDoubles;

/// <summary>Records every logged message instead of writing to the console, so tests can assert on narration.</summary>
public sealed class FakeCombatLogger : ICombatLogger
{
    public List<(string Message, LogColor Color)> Entries { get; } = [];

    public void Log(string message, LogColor color = LogColor.Default)
        => Entries.Add((message, color));

    public bool Any(Func<string, bool> predicate) => Entries.Any(e => predicate(e.Message));
}
