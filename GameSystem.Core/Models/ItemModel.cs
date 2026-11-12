namespace GameSystem.Core.Models;

/// <summary>Pure data model of an equippable item's stat modifiers.</summary>
public record ItemModel(string Name, int HealthBonus = 0, int ArmorBonus = 0, int AttackBonus = 0);
