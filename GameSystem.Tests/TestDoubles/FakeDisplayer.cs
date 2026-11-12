using GameSystem.Core.Abstractions;

namespace GameSystem.Tests.TestDoubles;

/// <summary>Records every displayed item instead of writing to the console, so tests can assert on CLI feedback.</summary>
public sealed class FakeDisplayer : IDisplayer
{
    public List<(DisplayKind Kind, string Text)> Entries { get; } = [];

    public void Display(IDisplayable item) => Entries.Add((item.Kind, item.Text));

    public IEnumerable<string> Errors => Entries.Where(e => e.Kind == DisplayKind.Error).Select(e => e.Text);
    public IEnumerable<string> Successes => Entries.Where(e => e.Kind == DisplayKind.Success).Select(e => e.Text);

    public bool HasError(string containing) => Errors.Any(t => t.Contains(containing, StringComparison.OrdinalIgnoreCase));
    public bool HasSuccess(string containing) => Successes.Any(t => t.Contains(containing, StringComparison.OrdinalIgnoreCase));
    public bool Any(string containing) => Entries.Any(e => e.Text.Contains(containing, StringComparison.OrdinalIgnoreCase));
}
