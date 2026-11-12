using GameSystem.Core.Models;
using GameSystem.Infra.Dto;

namespace GameSystem.Infra.Mapping;

/// <summary>Converts between Core's persistence models and the JSON-facing DTOs.</summary>
public static class GameStateMapper
{
    public static GameStateDto ToDto(GameStateModel model) => new()
    {
        Items = [.. model.Items.Select(i => new ItemDto
        {
            Name = i.Name,
            HealthBonus = i.HealthBonus,
            ArmorBonus = i.ArmorBonus,
            AttackBonus = i.AttackBonus
        })],
        Abilities = [.. model.Abilities.Select(a => new AbilityDto
        {
            Id = a.Id,
            Description = a.Description,
            Power = a.Power,
            IgnoresArmor = a.IgnoresArmor
        })],
        Characters = [.. model.Characters.Select(c => new CharacterDto
        {
            ClassId = c.ClassId,
            Name = c.Name,
            Health = c.Health,
            EquippedItemName = c.EquippedItemName,
            AbilityIds = [.. c.AbilityIds]
        })]
    };

    public static GameStateModel ToModel(GameStateDto dto) => new(
        Items: [.. dto.Items.Select(i => new ItemModel(i.Name, i.HealthBonus, i.ArmorBonus, i.AttackBonus))],
        Abilities: [.. dto.Abilities.Select(a => new AbilityModel(a.Id, a.Description, a.Power, a.IgnoresArmor))],
        Characters: [.. dto.Characters.Select(c => new CharacterStateModel(c.ClassId, c.Name, c.Health, c.EquippedItemName, [.. c.AbilityIds]))]
    );
}
