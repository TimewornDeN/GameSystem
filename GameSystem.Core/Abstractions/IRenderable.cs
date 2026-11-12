namespace GameSystem.Core.Abstractions;

/// <summary>
/// Something that can show itself with a renderer. The object decides WHAT data is shown
/// (<typeparamref name="T"/>), the renderer decides HOW it looks.
/// </summary>
public interface IRenderable<T>
{
    /// <summary>Remembers a default renderer that <see cref="Render()"/> will use.</summary>
    void UseRenderer(IRenderer<T> renderer);

    /// <summary>Shows the data with the given renderer.</summary>
    void Render(IRenderer<T> renderer);

    /// <summary>Shows the data with the renderer set by <see cref="UseRenderer"/>.</summary>
    void Render();
}
