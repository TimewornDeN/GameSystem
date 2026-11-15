using GameSystem.Core.Abstractions;

namespace GameSystem.Tests.TestDoubles;

/// <summary>Feeds a pre-scripted sequence of answers to code that reads input line by line, ignoring prompts.</summary>
public sealed class FakeInputReader(params string?[] answers) : IInputReader
{
    private readonly Queue<string?> _answers = new(answers);

    public string? ReadLine(string? prompt = null)
        => _answers.Count > 0 ? _answers.Dequeue() : null;
}
