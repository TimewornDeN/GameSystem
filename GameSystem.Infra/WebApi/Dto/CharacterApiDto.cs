using System.Text.Json.Serialization;

namespace GameSystem.Infra.WebApi.Dto;

/// <summary>Shape of one entry under GET /characters/{id} on genshin.jmp.blue (fields we actually use).</summary>
public sealed class CharacterApiDto
{
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Vision { get; set; }
    public string? Weapon { get; set; }
    public int Rarity { get; set; }
    public string? Description { get; set; }

    [JsonPropertyName("skillTalents")]
    public List<SkillTalentApiDto> SkillTalents { get; set; } = [];
}

public sealed class SkillTalentApiDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
