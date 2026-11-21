using GameSystem.Core.Combat;
using GameSystem.Core.Engine;
using GameSystem.Core.Models;
using GameSystem.Infra.Dto;

namespace GameSystem.Infra.Mapping;

/// <summary>Converts between Core's persistence models and the JSON-facing DTOs.</summary>
public static class GameStateMapper
{
    public static GameStateDto ToDto(GameStateModel model) => new()
    {
        Version = GameStateDto.CurrentVersion,
        Items = [.. model.Items.Select(i => new ItemDto
        {
            Name = i.Name,
            HealthBonus = i.HealthBonus,
            ArmorBonus = i.ArmorBonus,
            AttackBonus = i.AttackBonus,
            ResistBonus = i.ResistBonus,
            SpeedBonus = i.SpeedBonus
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
            AbilityIds = [.. c.AbilityIds],
            IsDefending = c.IsDefending,
            Effects = c.Effects is { Count: > 0 } effects
                ? effects.Select(e => new StatusEffectDto { Name = e.Name, Stat = e.Stat.ToString(), Amount = e.Amount, Turns = e.Turns }).ToList()
                : null
        })],
        Battle = model.Battle is null ? null : ToDto(model.Battle),
        RemoteInfo = model.RemoteInfo is { Count: > 0 }
            ? model.RemoteInfo.ToDictionary(p => p.Key, p => ToDto(p.Value))
            : null
    };

    private static BattleDto ToDto(BattleStateModel battle) => new()
    {
        FirstName = battle.FirstName,
        SecondName = battle.SecondName,
        SecondFighterIsAi = battle.SecondFighterIsAi,
        Round = battle.Round,
        TurnsPlayed = battle.TurnsPlayed,
        CurrentOrder = [.. battle.CurrentOrder],
        History = [.. battle.History.Select(e => new TurnEntryDto
        {
            Round = e.Round,
            Sequence = e.Sequence,
            Actor = e.Actor,
            Opponent = e.Opponent,
            Action = e.Action,
            ActorHealthBefore = e.ActorHealthBefore,
            ActorHealthAfter = e.ActorHealthAfter,
            OpponentHealthBefore = e.OpponentHealthBefore,
            OpponentHealthAfter = e.OpponentHealthAfter
        })]
    };

    private static RemoteCharacterDto ToDto(RemoteCharacterInfo info) => new()
    {
        Id = info.Id,
        Name = info.Name,
        Title = info.Title,
        Element = info.Element,
        WeaponType = info.WeaponType,
        Rarity = info.Rarity,
        Description = info.Description,
        Abilities = [.. info.Abilities.Select(a => new RemoteAbilityDto { Name = a.Name, Description = a.Description })]
    };

    public static GameStateModel ToModel(GameStateDto dto) => new(
        Items: [.. dto.Items.Select(i => new ItemModel(i.Name, i.HealthBonus, i.ArmorBonus, i.AttackBonus, i.ResistBonus, i.SpeedBonus))],
        Abilities: [.. dto.Abilities.Select(a => new AbilityModel(a.Id, a.Description, a.Power, a.IgnoresArmor))],
        Characters: [.. dto.Characters.Select(c => new CharacterStateModel(
            c.ClassId, c.Name, c.Health, c.EquippedItemName, [.. c.AbilityIds],
            c.IsDefending,
            c.Effects is { Count: > 0 } ? c.Effects.Select(ToEffect).ToList() : null))],
        Battle: dto.Battle is null ? null : ToModel(dto.Battle),
        RemoteInfo: dto.RemoteInfo is { Count: > 0 }
            ? dto.RemoteInfo.ToDictionary(p => p.Key, p => ToModel(p.Value))
            : null
    );

    private static StatusEffect ToEffect(StatusEffectDto dto)
        => Enum.TryParse<StatKind>(dto.Stat, ignoreCase: true, out var stat) && Enum.IsDefined(stat)
            ? new StatusEffect(dto.Name, stat, dto.Amount, dto.Turns)
            : throw new ArgumentException($"Unknown stat '{dto.Stat}' in effect '{dto.Name}'.");

    private static BattleStateModel ToModel(BattleDto dto) => new(
        dto.FirstName,
        dto.SecondName,
        dto.SecondFighterIsAi,
        dto.Round,
        dto.TurnsPlayed,
        [.. dto.CurrentOrder],
        [.. dto.History.Select(e => new TurnEntry(
            e.Round, e.Sequence, e.Actor, e.Opponent, e.Action,
            e.ActorHealthBefore, e.ActorHealthAfter, e.OpponentHealthBefore, e.OpponentHealthAfter))]);

    private static RemoteCharacterInfo ToModel(RemoteCharacterDto dto) => new(
        dto.Id, dto.Name, dto.Title, dto.Element, dto.WeaponType, dto.Rarity, dto.Description,
        [.. dto.Abilities.Select(a => new RemoteAbilityInfo(a.Name, a.Description))]);
}
