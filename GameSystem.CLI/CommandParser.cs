using GameSystem.CLI.Interfaces;
using System.Text.RegularExpressions;

namespace GameSystem.CLI;

/// <summary>
/// Parses CLI input strings into structured command contexts with validation and quote handling.
/// </summary>
public class CommandParser : ICommandParser
{
    public CommandContext Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new CommandContext(string.Empty);

        // Regex matches quoted strings ("heading title") or space-separated tokens
        var matches = Regex.Matches(input, @"[""].+?[""]|[^ ]+")
                           .Select(m => m.Value.Trim('"'))
                           .Where(s => !string.IsNullOrWhiteSpace(s))
                           .ToList();

        if (matches.Count == 0)
            return new CommandContext(string.Empty);

        string commandName = matches[0].ToLowerInvariant();
        var rawArgs = matches.Skip(1).ToList();

        var positionalArgs = new List<string>();
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rawArgs.Count; i++)
        {
            string token = rawArgs[i];

            if (token.StartsWith('-'))
            {
                string key = token.TrimStart('-');
                if (string.IsNullOrEmpty(key)) continue;

                // Check if the next token is an explicit value or another flag option
                if (i + 1 < rawArgs.Count && !rawArgs[i + 1].StartsWith('-'))
                {
                    options[key] = rawArgs[i + 1];
                    i++; // Skip option value
                }
                else
                {
                    options[key] = "true"; // Flag without value
                }
            }
            else
            {
                positionalArgs.Add(token);
            }
        }

        return new CommandContext(commandName, positionalArgs, options);
    }
}