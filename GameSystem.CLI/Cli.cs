using GameSystem.CLI.Interfaces;
using GameSystem.Core.Abstractions;

namespace GameSystem.CLI;

/// <summary>
/// FACADE of the command interface. The outside code talks only to this class:
/// <list type="bullet">
/// <item><see cref="UseStrategy"/> plugs in a group of commands,</item>
/// <item><see cref="ExecCommand"/> parses a raw line and runs the command in the strategy that owns it,</item>
/// <item><see cref="Display{T}"/> shows a renderable object with the renderer that was registered for its data type.</item>
/// </list>
/// </summary>
public sealed class Cli(ICommandParser parser, IDisplayer displayer) : ICommandCatalog, IRenderableDisplay
{
    private readonly ICommandParser _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    private readonly IDisplayer _displayer = displayer ?? throw new ArgumentNullException(nameof(displayer));
    private readonly List<ICommandStrategy> _strategies = [];
    private readonly Dictionary<Type, object> _renderers = [];

    public IReadOnlyList<ICommandStrategy> Strategies => _strategies;

    /// <summary>Plugs in a strategy. Two strategies cannot own the same command name.</summary>
    public void UseStrategy(ICommandStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);

        foreach (string selector in strategy.GetCommandSelectors())
        {
            if (FindStrategy(selector) is { } owner)
                throw new InvalidOperationException($"Command '{selector}' is already provided by strategy '{owner.Name}'.");
        }

        _strategies.Add(strategy);
    }

    /// <summary>Registers how data of type <typeparamref name="T"/> is shown by <see cref="Display{T}"/>.</summary>
    public void UseRenderer<T>(IRenderer<T> renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        _renderers[typeof(T)] = renderer;
    }

    /// <summary>
    /// Parses and executes one line. Returns false when no strategy knows the command.
    /// Errors thrown by a command never escape: they are reported through the displayer.
    /// </summary>
    public bool ExecCommand(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
            return true;

        var args = _parser.Parse(rawInput);
        if (string.IsNullOrEmpty(args.CommandName))
            return true;

        var strategy = FindStrategy(args.CommandName);
        if (strategy is null)
        {
            _displayer.Warning($"Unknown command '{args.CommandName}'. Type 'help' for the list of commands.");
            return false;
        }

        try
        {
            strategy.ExecuteCommand(args.CommandName, args);
        }
        catch (Exception ex)
        {
            _displayer.Error($"Execution Error: {ex.Message}");
        }

        return true;
    }

    /// <summary>
    /// Shows the object with the renderer registered for <typeparamref name="T"/>.
    /// Without a registered renderer the object's own renderer (see <see cref="IRenderable{T}.UseRenderer"/>) is used.
    /// </summary>
    public void Display<T>(IRenderable<T> renderable)
    {
        ArgumentNullException.ThrowIfNull(renderable);

        if (_renderers.TryGetValue(typeof(T), out var renderer))
            renderable.Render((IRenderer<T>)renderer);
        else
            renderable.Render();
    }

    private ICommandStrategy? FindStrategy(string selector)
        => _strategies.FirstOrDefault(s => s.GetCommandSelectors().Contains(selector, StringComparer.OrdinalIgnoreCase));
}
