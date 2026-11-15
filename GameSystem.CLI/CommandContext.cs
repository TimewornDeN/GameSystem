namespace GameSystem.CLI;

/// <summary>
/// Represents parsed command context containing positional arguments and key-value options.
/// </summary>
public class CommandContext(string commandName, List<string>? args = null, Dictionary<string, string>? options = null)
{
    public string CommandName { get; } = commandName ?? string.Empty;
    public List<string> Args { get; } = args ?? [];
    public Dictionary<string, string> Options { get; } = options ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks whether a specific option key exists.
    /// </summary>
    public bool HasOption(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        return Options.ContainsKey(key);
    }

    /// <summary>
    /// Gets the value of a specific option key or null if not found.
    /// </summary>
    public string? GetOption(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        return Options.TryGetValue(key, out var val) ? val : null;
    }
}
