using GameSystem.Core.Abstractions;

namespace GameSystem.Infra.Display;

/// <summary>Displays CLI feedback into any TextWriter (file, StringWriter in tests...), without colors.</summary>
public class TextWriterDisplayer(TextWriter writer) : IDisplayer
{
    private readonly TextWriter _writer = writer ?? throw new ArgumentNullException(nameof(writer));

    public void Display(IDisplayable item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _writer.WriteLine(DisplayStyle.Prefix(item.Kind) + item.Text);
    }
}
