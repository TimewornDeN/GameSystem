namespace GameSystem.Core.Abstractions;

/// <summary>
/// Knows how to present data of type <typeparamref name="T"/> to the user.
/// The core only declares this interface; the presentation layer (the CLI) implements it,
/// so the dependency points from the UI to the core and never the other way round.
/// </summary>
public interface IRenderer<in T>
{
    void Render(T data);
}
