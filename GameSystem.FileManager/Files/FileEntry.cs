namespace GameSystem.FileManager.Files;

/// <summary>One item of a directory listing.</summary>
public sealed record FileEntry(string Name, string FullPath, bool IsDirectory, long Size);
