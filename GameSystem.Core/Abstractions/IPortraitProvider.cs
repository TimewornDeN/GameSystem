namespace GameSystem.Core.Abstractions;

/// <summary>
/// Source of character portraits (images). Core only knows the contract; the real implementation downloads
/// the picture from the remote character database and keeps a copy on disk.
/// </summary>
public interface IPortraitProvider
{
    /// <summary>
    /// Returns the raw bytes of the portrait of a remote character, or null when there is no portrait
    /// (unknown id, no network): the caller shows a placeholder then.
    /// </summary>
    Task<byte[]?> GetPortraitAsync(string remoteId, CancellationToken ct = default);

    /// <summary>
    /// Full path of the file where the portrait is kept on the disk, or null when it is not there (yet) or the
    /// provider keeps no files. Used to tell the user where the downloaded picture lies.
    /// </summary>
    string? GetLocalPath(string remoteId) => null;
}
