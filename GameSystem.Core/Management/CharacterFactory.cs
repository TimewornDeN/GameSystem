using GameSystem.Core.Characters;
using GameSystem.Core.Logging;

namespace GameSystem.Core.Management;

/// <summary>Creates characters by class id, decoupling callers (CLI, persistence) from concrete character types.</summary>
public static class CharacterFactory
{
    public static Character Create(string classId, string name, ICombatLogger logger)
    {
        if (string.IsNullOrWhiteSpace(classId))
            throw new ArgumentException("Class id cannot be empty.", nameof(classId));

        return classId.Trim().ToLowerInvariant() switch
        {
            "warrior" => new Warrior(name, logger),
            "mage" => new Mage(name, logger),
            _ => throw new ArgumentException($"Unknown character class '{classId}'. Allowed: warrior, mage.", nameof(classId))
        };
    }
}
