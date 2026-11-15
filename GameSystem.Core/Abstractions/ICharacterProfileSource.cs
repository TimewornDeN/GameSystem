using GameSystem.Core.Models;

namespace GameSystem.Core.Abstractions;

/// <summary>Where new characters come from (a folder of JSON files in the real app, a stub in tests).</summary>
public interface ICharacterProfileSource
{
    /// <summary>Names of the profiles that can be loaded.</summary>
    IReadOnlyList<string> List();

    /// <summary>Reads one profile by name.</summary>
    /// <exception cref="FileNotFoundException">There is no such profile.</exception>
    /// <exception cref="InvalidDataException">The profile exists but cannot be read.</exception>
    CharacterProfile Load(string name);
}
