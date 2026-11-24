using GameSystem.Core.Abstractions;

namespace GameSystem.Infra.WebApi;

/// <summary>
/// Downloads character portraits from the remote character database (<c>characters/{id}/icon-big</c>) and keeps
/// every downloaded picture in a cache folder, so a portrait is fetched only once and also works offline later.
/// The file gets the extension of its real image format (<c>.png</c>, <c>.jpg</c>...), so the pictures in the
/// folder can be opened with any image viewer.
/// A missing portrait is not an error: the method returns null and the window shows a placeholder.
/// </summary>
public sealed class HttpPortraitProvider(HttpClient httpClient, string cacheDirectory) : IPortraitProvider
{
    // ".img" is the extension of unknown formats and of the files of earlier versions of the program.
    private static readonly string[] KnownExtensions = [".png", ".jpg", ".webp", ".gif", ".img"];

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly string _cacheDirectory = string.IsNullOrWhiteSpace(cacheDirectory)
        ? throw new ArgumentException("Cache directory cannot be empty.", nameof(cacheDirectory))
        : cacheDirectory;

    public async Task<byte[]?> GetPortraitAsync(string remoteId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(remoteId))
            return null;

        try
        {
            if (GetLocalPath(remoteId) is { } cached)
                return await File.ReadAllBytesAsync(cached, ct);

            using var response = await _httpClient.GetAsync($"characters/{Uri.EscapeDataString(remoteId)}/icon-big", ct);
            if (!response.IsSuccessStatusCode)
                return null;

            byte[] bytes = await response.Content.ReadAsByteArrayAsync(ct);
            if (bytes.Length == 0)
                return null;

            Cache(Path.Combine(_cacheDirectory, ToBaseName(remoteId) + ExtensionFor(bytes)), bytes);
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

    /// <summary>The file with the portrait in the cache folder, or null when it was not downloaded yet.</summary>
    public string? GetLocalPath(string remoteId)
    {
        if (string.IsNullOrWhiteSpace(remoteId))
            return null;

        string baseName = ToBaseName(remoteId);
        foreach (string extension in KnownExtensions)
        {
            string path = Path.Combine(_cacheDirectory, baseName + extension);
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static void Cache(string path, byte[] bytes)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporary = path + ".tmp";
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A failed cache write only means the picture will be downloaded again next time.
        }
    }

    /// <summary>The extension that matches the first bytes of the picture (PNG, JPEG, GIF, WebP), else ".img".</summary>
    private static string ExtensionFor(byte[] b)
    {
        if (b.Length >= 4 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47)
            return ".png";

        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            return ".jpg";

        if (b.Length >= 4 && b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x38)
            return ".gif";

        // WebP: "RIFF" + 4 bytes of size + "WEBP".
        if (b.Length >= 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46
            && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50)
            return ".webp";

        return ".img";
    }

    private static string ToBaseName(string remoteId)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        return new string([.. remoteId.Trim().ToLowerInvariant().Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c)]);
    }
}
