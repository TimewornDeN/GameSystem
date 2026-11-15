using GameSystem.Core.Abstractions;

namespace GameSystem.Infra.Display;

/// <summary>Displays CLI feedback (info/success/warning/error) in the console using colors.</summary>
public class ConsoleDisplayer : IDisplayer
{
    public void Display(IDisplayable item)
    {
        ArgumentNullException.ThrowIfNull(item);

        string line = DisplayStyle.Prefix(item.Kind) + item.Text;
        ConsoleColor? color = DisplayStyle.Color(item.Kind);

        if (color is null)
        {
            Console.WriteLine(line);
            return;
        }

        Console.ForegroundColor = color.Value;
        Console.WriteLine(line);
        Console.ResetColor();
    }
}
