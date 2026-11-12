using GameSystem.Core.Abstractions;
using GameSystem.Core.Rendering;
using Xunit;

namespace GameSystem.Tests.Core;

public class RenderableTests
{
    private sealed class RecordingRenderer : IRenderer<string>
    {
        public List<string> Rendered { get; } = [];

        public void Render(string data) => Rendered.Add(data);
    }

    [Fact]
    public void Render_WithRenderer_PassesTheData()
    {
        var renderer = new RecordingRenderer();

        new Renderable<string>("data").Render(renderer);

        Assert.Equal("data", Assert.Single(renderer.Rendered));
    }

    [Fact]
    public void Render_WithoutArguments_UsesTheDefaultRenderer()
    {
        var renderer = new RecordingRenderer();
        var renderable = new Renderable<string>("data");
        renderable.UseRenderer(renderer);

        renderable.Render();

        Assert.Equal("data", Assert.Single(renderer.Rendered));
    }

    [Fact]
    public void Render_WithoutAnyRenderer_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new Renderable<string>("data").Render());
    }

    [Fact]
    public void NullRenderers_AreRejected()
    {
        var renderable = new Renderable<string>("data");

        Assert.Throws<ArgumentNullException>(() => renderable.UseRenderer(null!));
        Assert.Throws<ArgumentNullException>(() => renderable.Render(null!));
    }
}
