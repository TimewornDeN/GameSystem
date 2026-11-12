namespace GameSystem.FileManager.Rendering;

/// <summary>A run of text that is drawn with a single <see cref="Style"/>.</summary>
public readonly record struct TextSpan(string Text, Style Style);
