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
    public bool IsDefending { get; set; }
    public List<StatusEffectDto>? Effects { get; set; }
}

/// <summary>Persistence DTO for a buff or debuff; the stat is stored as text so the file stays readable.</summary>
public sealed class StatusEffectDto
{
    public string Name { get; set; } = string.Empty;
    public string Stat { get; set; } = string.Empty;
    public int Amount { get; set; }
    public int Turns { get; set; }
}

/// <summary>Persistence DTO for one line of the battle history.</summary>
public sealed class TurnEntryDto
{
    public int Round { get; set; }
    public int Sequence { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Opponent { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int ActorHealthBefore { get; set; }
    public int ActorHealthAfter { get; set; }
    public int OpponentHealthBefore { get; set; }
    public int OpponentHealthAfter { get; set; }
}

/// <summary>Persistence DTO for a battle that is still going on.</summary>
public sealed class BattleDto
{
    public string FirstName { get; set; } = string.Empty;
    public string SecondName { get; set; } = string.Empty;
    public bool SecondFighterIsAi { get; set; }
    public int Round { get; set; }
    public int TurnsPlayed { get; set; }
    public List<int> CurrentOrder { get; set; } = [];
    public List<TurnEntryDto> History { get; set; } = [];
}

/// <summary>Persistence DTO for one talent of a character taken from the remote database.</summary>
public sealed class RemoteAbilityDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>Persistence DTO for the narrative data of a character taken from the remote database.</summary>
public sealed class RemoteCharacterDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Element { get; set; } = string.Empty;
    public string WeaponType { get; set; } = string.Empty;
    public int Rarity { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<RemoteAbilityDto> Abilities { get; set; } = [];
}

/// <summary>Root persistence DTO: the whole saved game world.</summary>
public sealed class GameStateDto
{
    /// <summary>Version of the file layout that this build writes. Version 1 files (no field) are still readable.</summary>
    public const int CurrentVersion = 2;

    /// <summary>Layout version of the file; 0 means the file was written before versions existed.</summary>
    public int Version { get; set; }
    public List<ItemDto> Items { get; set; } = [];
    public List<AbilityDto> Abilities { get; set; } = [];
    public List<CharacterDto> Characters { get; set; } = [];
    public BattleDto? Battle { get; set; }
    public Dictionary<string, RemoteCharacterDto>? RemoteInfo { get; set; }
}
