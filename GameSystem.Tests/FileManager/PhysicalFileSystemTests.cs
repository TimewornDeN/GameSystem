using Xunit;
using GameSystem.FileManager.Infrastructure;

namespace GameSystem.Tests.FileManager;

public sealed class PhysicalFileSystemTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "fm-tests-" + Guid.NewGuid().ToString("N"));
    private readonly PhysicalFileSystem _fileSystem = new();

    public PhysicalFileSystemTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "sub"));
        File.WriteAllText(Path.Combine(_root, "b.txt"), "hello");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void GetEntries_ListsDirectoriesFirst()
    {
        var entries = _fileSystem.GetEntries(_root);

        Assert.Equal(2, entries.Count);
        Assert.True(entries[0].IsDirectory);
        Assert.Equal("sub", entries[0].Name);
        Assert.Equal("b.txt", entries[1].Name);
        Assert.Equal(5, entries[1].Size);
    }

    [Fact]
    public void WriteLines_ThenReadLines_RoundTrips()
    {
        var path = Path.Combine(_root, "c.txt");

        _fileSystem.WriteLines(path, ["один", "two"]);

        Assert.Equal("один|two", string.Join('|', _fileSystem.ReadLines(path)));
    }

    [Fact]
    public void GetParent_ReturnsTheContainingDirectory()
    {
        var parent = _fileSystem.GetParent(Path.Combine(_root, "sub"));

        Assert.Equal(Path.GetFullPath(_root), parent);
    }

    [Fact]
    public void ResolvePath_HandlesRelativeParentAndAbsolutePaths()
    {
        var sub = Path.Combine(_root, "sub");

        Assert.Equal(sub, _fileSystem.ResolvePath(_root, "sub"));
        Assert.Equal(Path.GetFullPath(_root), _fileSystem.ResolvePath(sub, ".."));
        Assert.Equal(sub, _fileSystem.ResolvePath("unrelated", sub));
    }

    [Fact]
    public void GetEntries_OfMissingDirectory_ThrowsIOException()
    {
        Assert.Throws<DirectoryNotFoundException>(() => _fileSystem.GetEntries(Path.Combine(_root, "nope")));
    }
}
