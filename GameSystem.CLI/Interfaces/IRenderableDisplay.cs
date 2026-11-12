using GameSystem.Core.Abstractions;

namespace GameSystem.CLI.Interfaces;

/// <summary>The part of the CLI that commands need to show renderable objects. Commands do not depend on the whole facade.</summary>
public interface IRenderableDisplay
{
    void Display<T>(IRenderable<T> renderable);
}
