namespace GameSystem.Core.Models;

/// <summary>Pure data model of an ability with a fixed power value.</summary>
public record AbilityModel(string Id, string Description, int Power, bool IgnoresArmor = false);
