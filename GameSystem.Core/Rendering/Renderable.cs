using GameSystem.Core.Abstractions;

namespace GameSystem.Core.Rendering;

/// <summary>Base implementation of <see cref="IRenderable{T}"/>: it holds the data and an optional default renderer.</summary>
public class Renderable<T>(T data) : IRenderable<T>
{
    private IRenderer<T>? _renderer;

    public T Data { get; } = data;

    public void UseRenderer(IRenderer<T> renderer)
        => _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

    public void Render(IRenderer<T> renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        renderer.Render(Data);
    }

    public void Render()
    {
        if (_renderer is null)
            throw new InvalidOperationException($"No renderer is assigned for {GetType().Name}. Call UseRenderer first.");

        _renderer.Render(Data);
    }
}
