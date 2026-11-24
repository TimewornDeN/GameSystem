using System.Text.Json;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Models;

namespace GameSystem.Core.Management;

/// <summary>What one update of the catalog produced.</summary>
/// <param name="Characters">Every character that was downloaded, sorted by name.</param>
/// <param name="Failed">How many characters the server listed but could not be downloaded.</param>
public sealed record CatalogRefreshResult(IReadOnlyList<RemoteCharacterInfo> Characters, int Failed);

/// <summary>
/// The catalog of all characters the remote database offers. It downloads the list and the details of each
/// character, parses them (the parsing itself is done by <see cref="IGenshinApiClient"/>) and keeps the result in
/// a local store, so the next start does not need the network.
/// </summary>
public sealed class CharacterCatalog(IGenshinApiClient client, ICharacterCatalogStore store)
{
    /// <summary>The server is asked for a few characters at the same time, not for all of them at once.</summary>
    private const int MaxParallelRequests = 6;

    private readonly IGenshinApiClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly ICharacterCatalogStore _store = store ?? throw new ArgumentNullException(nameof(store));

    /// <summary>The catalog saved on the disk by the last update (empty when there was none).</summary>
    public IReadOnlyList<RemoteCharacterInfo> Cached() => _store.Load();

    /// <summary>
    /// Downloads the whole catalog and saves it. A character that cannot be downloaded is skipped and counted,
    /// the rest is kept. When nothing could be downloaded the saved catalog stays as it was.
    /// </summary>
    public async Task<CatalogRefreshResult> RefreshAsync(CancellationToken ct = default)
    {
        IReadOnlyList<string> ids = await _client.GetCharacterIdsAsync(ct);
        using var gate = new SemaphoreSlim(MaxParallelRequests);

        async Task<RemoteCharacterInfo?> Fetch(string id)
        {
            await gate.WaitAsync(ct);
            try
            {
                return await _client.GetCharacterAsync(id, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or JsonException or NotSupportedException)
            {
                return null;
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                // The request timed out: this character is skipped like any other that could not be downloaded.
                return null;
            }
            finally
            {
                gate.Release();
            }
        }

        RemoteCharacterInfo?[] fetched = await Task.WhenAll(ids.Select(Fetch));

        List<RemoteCharacterInfo> characters =
        [
            .. fetched
                .OfType<RemoteCharacterInfo>()
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
        ];

        if (characters.Count > 0)
            _store.Save(characters);

        return new CatalogRefreshResult(characters, ids.Count - characters.Count);
    }

    /// <summary>Finds a character of the catalog by its id or by its name (case does not matter).</summary>
    public static RemoteCharacterInfo? Find(IEnumerable<RemoteCharacterInfo> characters, string idOrName)
    {
        ArgumentNullException.ThrowIfNull(characters);

        if (string.IsNullOrWhiteSpace(idOrName))
            return null;

        string key = idOrName.Trim();
        return characters.FirstOrDefault(c => string.Equals(c.Id, key, StringComparison.OrdinalIgnoreCase))
            ?? characters.FirstOrDefault(c => string.Equals(c.Name, key, StringComparison.OrdinalIgnoreCase));
    }
}
