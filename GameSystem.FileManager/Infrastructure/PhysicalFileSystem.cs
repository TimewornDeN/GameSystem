using System.Text;
using GameSystem.FileManager.Files;

namespace GameSystem.FileManager.Infrastructure;

/// <summary>The real disk. All IO exceptions are passed up to the modes, which show them in the status line.</summary>
public sealed class PhysicalFileSystem : IFileSystem
{
    public IReadOnlyList<FileEntry> GetEntries(string directory)
    {
        var info = new DirectoryInfo(directory);

        var directories = info.EnumerateDirectories()
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => new FileEntry(item.Name, item.FullName, true, 0));

        var files = info.EnumerateFiles()
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => new FileEntry(item.Name, item.FullName, false, item.Length));

        // ToList() forces the enumeration here, so access errors are thrown inside this method.
        return directories.Concat(files).ToList();
    }

    public string? GetParent(string directory) => Directory.GetParent(directory)?.FullName;

    public string ResolvePath(string baseDirectory, string path) =>
        Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(path, baseDirectory);

    public IReadOnlyList<string> ReadLines(string path) => File.ReadAllLines(path);

    public void WriteLines(string path, IReadOnlyList<string> lines) =>
        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}
