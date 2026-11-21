namespace GameSystem.Infra.Persistence;

/// <summary>
/// Writes a file so that it is either completely the old version or completely the new one: the text goes to a
/// temporary neighbour first and only then replaces the real file.
/// </summary>
public static class AtomicFile
{
    public static void WriteAllText(string path, string contents)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(contents);

        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string temporary = path + ".tmp";
        File.WriteAllText(temporary, contents);
        File.Move(temporary, path, overwrite: true);
    }
}
