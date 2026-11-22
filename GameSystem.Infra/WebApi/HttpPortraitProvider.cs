using GameSystem.Core.Abstractions;

namespace GameSystem.Infra.WebApi;

/// <summary>
/// Downloads character portraits from the remote character database (<c>characters/{id}/icon-big</c>) and keeps
/// every downloaded picture in a cache folder, so a portrait is fetched only once and also works offline later.
/// A missing portrait is not an error: the method returns null and the window shows a placeholder.
/// </summary>
public sealed class HttpPortraitProvider(HttpClient httpClient, string cacheDirectory) : IPortraitProvider
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly string _cacheDirectory = string.IsNullOrWhiteSpace(cacheDirectory)
        ? throw new ArgumentException("Cache directory cannot be empty.", nameof(cacheDirectory))
        : cacheDirectory;

    public async Task<byte[]?> GetPortraitAsync(string remoteId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(remoteId))
            return null;

        string path = Path.Combine(_cacheDirectory, ToFileName(remoteId));

        try
        {
            if (File.Exists(path))
                return await File.ReadAllBytesAsync(path, ct);

            using var response = await _httpClient.GetAsync($"characters/{Uri.EscapeDataString(remoteId)}/icon-big", ct);
            if (!response.IsSuccessStatusCode)
                return null;

            byte[] bytes = await response.Content.ReadAsByteArrayAsync(ct);
            if (bytes.Length == 0)
                return null;

            Cache(path, bytes);
            return bytes;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            // The request timed out: same as no network.
            return null;
        }
    }

    private void Cache(string path, byte[] bytes)
    {
        try
        {
            Directory.CreateDirectory(_cacheDirectory);
            string temporary = path + ".tmp";
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A failed cache write only means the picture will be downloaded again next time.
        }
    }

    private static string ToFileName(string remoteId)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string safe = new string([.. remoteId.Trim().ToLowerInvariant().Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c)]);
        return safe + ".img";
    }
}
