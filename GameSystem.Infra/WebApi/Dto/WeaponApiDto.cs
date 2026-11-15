namespace GameSystem.Infra.WebApi.Dto;

/// <summary>Shape of one entry under GET /weapons/{id} on genshin.jmp.blue (fields we actually use).</summary>
public sealed class WeaponApiDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Rarity { get; set; }
    public int BaseAttack { get; set; }
    public string? PassiveName { get; set; }
    public string? PassiveDesc { get; set; }
    public string? Description { get; set; }
}
