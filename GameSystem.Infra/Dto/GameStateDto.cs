namespace GameSystem.Infra.Dto;

/// <summary>Persistence DTO for an item's stat modifiers.</summary>
public sealed class ItemDto
{
    public string Name { get; set; } = string.Empty;
    public int HealthBonus { get; set; }
    public int ArmorBonus { get; set; }
    public int AttackBonus { get; set; }
    public int ResistBonus { get; set; }
    public int SpeedBonus { get; set; }
}

/// <summary>Persistence DTO for a custom, fixed-power ability.</summary>
public sealed class AbilityDto
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Power { get; set; }
    public bool IgnoresArmor { get; set; }
}

/// <summary>Persistence DTO for one character: class, vitals, equipped item and known ability ids.</summary>
public sealed class CharacterDto
{
    public string ClassId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Health { get; set; }
    public string? EquippedItemName { get; set; }
    public List<string> AbilityIds { get; set; } = [];
}

/// <summary>Root persistence DTO: the whole saved game world.</summary>
public sealed class GameStateDto
{
    public List<ItemDto> Items { get; set; } = [];
    public List<AbilityDto> Abilities { get; set; } = [];
    public List<CharacterDto> Characters { get; set; } = [];
}
