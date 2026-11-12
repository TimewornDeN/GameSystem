using GameSystem.Core.Models;
using GameSystem.Infra.Persistence;
using Xunit;

namespace GameSystem.Tests.Infra;

public sealed class JsonGameResultRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "gs-results-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "nested", "game-results.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private static GameResult Sample(string winner) =>
        new(winner, "Loser", 3, 42, 150, new DateTime(2026, 11, 10, 12, 30, 0, DateTimeKind.Utc));

    [Fact]
    public void GetAll_WithoutFile_ReturnsEmptyList()
    {
        Assert.Empty(new JsonGameResultRepository(FilePath).GetAll());
    }

    [Fact]
    public void Add_CreatesTheFolder_AndRoundTripsTheResult()
    {
        var repository = new JsonGameResultRepository(FilePath);

        repository.Add(Sample("Grosh"));

        var loaded = Assert.Single(new JsonGameResultRepository(FilePath).GetAll());
        Assert.Equal(Sample("Grosh"), loaded);
    }

    [Fact]
    public void Add_Twice_KeepsBothInOrder()
    {
        var repository = new JsonGameResultRepository(FilePath);

        repository.Add(Sample("First"));
        repository.Add(Sample("Second"));

        Assert.Equal("First,Second", string.Join(',', repository.GetAll().Select(r => r.Winner)));
    }

    [Fact]
    public void GetAll_CorruptedFile_ThrowsInvalidDataException()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, "{ this is not json");

        Assert.Throws<InvalidDataException>(() => new JsonGameResultRepository(FilePath).GetAll());
    }

    [Fact]
    public void Constructor_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => new JsonGameResultRepository(" "));
    }
}
