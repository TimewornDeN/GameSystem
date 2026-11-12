using GameSystem.Core.Abstractions;
using GameSystem.Core.Models;

namespace GameSystem.Tests.TestDoubles;

/// <summary>In-memory stand-in for the remote character database, so CLI tests don't hit the network.</summary>
public sealed class FakeGenshinApiClient : IGenshinApiClient
{
    private readonly Dictionary<string, RemoteCharacterInfo> _characters = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RemoteWeaponInfo> _weapons = new(StringComparer.OrdinalIgnoreCase);

    public void AddCharacter(RemoteCharacterInfo info) => _characters[info.Id] = info;
    public void AddWeapon(RemoteWeaponInfo info) => _weapons[info.Id] = info;

    public Task<IReadOnlyList<string>> GetCharacterIdsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>([.. _characters.Keys.Order()]);

    public Task<RemoteCharacterInfo> GetCharacterAsync(string id, CancellationToken ct = default)
        => _characters.TryGetValue(id, out var info)
            ? Task.FromResult(info)
            : Task.FromException<RemoteCharacterInfo>(new InvalidOperationException($"Unknown remote character '{id}'."));

    public Task<IReadOnlyList<string>> GetWeaponIdsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>([.. _weapons.Keys.Order()]);

    public Task<RemoteWeaponInfo> GetWeaponAsync(string id, CancellationToken ct = default)
        => _weapons.TryGetValue(id, out var info)
            ? Task.FromResult(info)
            : Task.FromException<RemoteWeaponInfo>(new InvalidOperationException($"Unknown remote weapon '{id}'."));
}
