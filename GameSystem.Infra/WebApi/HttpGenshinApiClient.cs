using System.Net.Http.Json;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Models;
using GameSystem.Infra.WebApi.Dto;

namespace GameSystem.Infra.WebApi;

/// <summary>
/// Fetches character and weapon data from the genshin.jmp.blue API (or any API-compatible mirror),
/// and adapts the raw JSON shape to Core's presentation-agnostic Remote*Info models.
/// </summary>
public class HttpGenshinApiClient(HttpClient httpClient) : IGenshinApiClient
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public async Task<IReadOnlyList<string>> GetCharacterIdsAsync(CancellationToken ct = default)
    {
        var ids = await _httpClient.GetFromJsonAsync<List<string>>("characters", ct)
            ?? throw new InvalidOperationException("Remote API returned no character list.");
        return ids;
    }

    public async Task<RemoteCharacterInfo> GetCharacterAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Character id cannot be empty.", nameof(id));

        var dto = await _httpClient.GetFromJsonAsync<CharacterApiDto>($"characters/{id}?lang=en", ct)
            ?? throw new InvalidOperationException($"Remote API returned no data for character '{id}'.");

        var abilities = dto.SkillTalents
            .Where(t => !string.IsNullOrWhiteSpace(t.Name))
            .Select(t => new RemoteAbilityInfo(t.Name, t.Description ?? string.Empty))
            .ToList();

        return new RemoteCharacterInfo(
            Id: id,
            Name: string.IsNullOrWhiteSpace(dto.Name) ? id : dto.Name,
            Title: dto.Title ?? string.Empty,
            Element: dto.Vision ?? string.Empty,
            WeaponType: dto.Weapon ?? string.Empty,
            Rarity: dto.Rarity,
            Description: dto.Description ?? string.Empty,
            Abilities: abilities);
    }

    public async Task<IReadOnlyList<string>> GetWeaponIdsAsync(CancellationToken ct = default)
    {
        var ids = await _httpClient.GetFromJsonAsync<List<string>>("weapons", ct)
            ?? throw new InvalidOperationException("Remote API returned no weapon list.");
        return ids;
    }

    public async Task<RemoteWeaponInfo> GetWeaponAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Weapon id cannot be empty.", nameof(id));

        var dto = await _httpClient.GetFromJsonAsync<WeaponApiDto>($"weapons/{id}?lang=en", ct)
            ?? throw new InvalidOperationException($"Remote API returned no data for weapon '{id}'.");

        return new RemoteWeaponInfo(
            Id: string.IsNullOrWhiteSpace(dto.Id) ? id : dto.Id,
            Name: string.IsNullOrWhiteSpace(dto.Name) ? id : dto.Name,
            Type: dto.Type ?? string.Empty,
            Rarity: dto.Rarity,
            BaseAttack: dto.BaseAttack,
            PassiveName: dto.PassiveName,
            PassiveDescription: dto.PassiveDesc,
            Description: dto.Description ?? string.Empty);
    }
}
