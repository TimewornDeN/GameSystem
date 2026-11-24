namespace GameSystem.Core.Models;

/// <summary>
/// Addresses of the pictures in the remote character database. The same address is shown to the user as the
/// link to the graphics of a character and used to download the portrait into the local cache,
/// so what the user can open in a browser is exactly the file that lies on the disk.
/// </summary>
public static class RemoteLinks
{
    public const string BaseAddress = "https://genshin.jmp.blue/";

    /// <summary>Link to the portrait of the character with the given id on the server.</summary>
    public static string Portrait(string characterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);
        return $"{BaseAddress}characters/{Uri.EscapeDataString(characterId)}/icon-big";
    }
}
