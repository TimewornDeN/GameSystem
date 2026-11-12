namespace GameSystem.FileManager.Files;

/// <summary>
/// File system abstraction. Implementations signal problems with <see cref="IOException"/>
/// or <see cref="UnauthorizedAccessException"/>; the modes turn those into status messages.
/// </summary>
public interface IFileSystem
{
    /// <summary>Lists a directory: sub-directories first, then files, both sorted by name.</summary>
    IReadOnlyList<FileEntry> GetEntries(string directory);

    /// <summary>Returns the parent directory, or <c>null</c> for a root.</summary>
    string? GetParent(string directory);

    /// <summary>Turns a user-typed path (absolute, relative, <c>..</c>) into a full path.</summary>
    string ResolvePath(string baseDirectory, string path);

    IReadOnlyList<string> ReadLines(string path);

    void WriteLines(string path, IReadOnlyList<string> lines);
}
