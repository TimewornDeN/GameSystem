using GameSystem.FileManager.Files;

namespace GameSystem.Tests.TestDoubles;

/// <summary>A tiny fake disk. Paths use '/' and a directory is registered together with its parent.</summary>
internal sealed class InMemoryFileSystem : IFileSystem
{
    private readonly Dictionary<string, string?> _directories = new();
    private readonly Dictionary<string, (string Directory, string Name, List<string> Lines)> _files = new();
    private readonly HashSet<string> _lockedDirectories = new();

    public bool ThrowOnWrite { get; set; }

    public void AddDirectory(string path, string? parent = null, bool locked = false)
    {
        _directories[path] = parent;
        if (locked)
        {
            _lockedDirectories.Add(path);
        }
    }

    public void AddFile(string directory, string name, params string[] lines) =>
        _files[$"{directory}/{name}"] = (directory, name, lines.ToList());

    public IReadOnlyList<string> GetFileContent(string path) => _files[path].Lines;

    public IReadOnlyList<FileEntry> GetEntries(string directory)
    {
        if (!_directories.ContainsKey(directory))
        {
            throw new DirectoryNotFoundException(directory);
        }

        if (_lockedDirectories.Contains(directory))
        {
            throw new UnauthorizedAccessException("Access denied");
        }

        var directories = _directories
            .Where(pair => pair.Value == directory)
            .Select(pair => new FileEntry(pair.Key[(pair.Key.LastIndexOf('/') + 1)..], pair.Key, true, 0))
            .OrderBy(entry => entry.Name, StringComparer.Ordinal);

        var files = _files.Values
            .Where(file => file.Directory == directory)
            .Select(file => new FileEntry(file.Name, $"{file.Directory}/{file.Name}", false, file.Lines.Sum(line => line.Length)))
            .OrderBy(entry => entry.Name, StringComparer.Ordinal);

        return directories.Concat(files).ToList();
    }

    public string? GetParent(string directory) => _directories.TryGetValue(directory, out var parent) ? parent : null;

    public string ResolvePath(string baseDirectory, string path)
    {
        if (path.StartsWith('/'))
        {
            return path;
        }

        var current = baseDirectory;
        foreach (var part in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part == "..")
            {
                current = GetParent(current) ?? current;
            }
            else if (part != ".")
            {
                current = $"{current}/{part}";
            }
        }

        return current;
    }

    public IReadOnlyList<string> ReadLines(string path) =>
        _files.TryGetValue(path, out var file) ? file.Lines.ToList() : throw new FileNotFoundException(path);

    public void WriteLines(string path, IReadOnlyList<string> lines)
    {
        if (ThrowOnWrite)
        {
            throw new IOException("disk full");
        }

        if (!_files.TryGetValue(path, out var file))
        {
            throw new FileNotFoundException(path);
        }

        _files[path] = (file.Directory, file.Name, lines.ToList());
    }
}
