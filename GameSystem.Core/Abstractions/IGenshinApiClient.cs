using GameSystem.Core.Models;

namespace GameSystem.Core.Abstractions;

/// <summary>
/// Access to the remote character/weapon database (genshin.jmp.blue or an equivalent API).
/// Core only knows about this contract; the actual HTTP call lives in Infra.
/// </summary>
public interface IGenshinApiClient
{
    Task<IReadOnlyList<string>> GetCharacterIdsAsync(CancellationToken ct = default);
    Task<RemoteCharacterInfo> GetCharacterAsync(string id, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetWeaponIdsAsync(CancellationToken ct = default);
    Task<RemoteWeaponInfo> GetWeaponAsync(string id, CancellationToken ct = default);
}
