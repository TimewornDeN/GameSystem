using System.Text.Json;
using GameSystem.CLI.Graphics;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Management;
using GameSystem.Core.Models;

namespace GameSystem.CLI.Commands;

/// <summary>
/// catalog [list] | catalog refresh | catalog info &lt;id|name&gt; | catalog portraits — the catalog of all characters
/// that the remote database offers. The list is kept on the disk, so it opens at once and works without the network;
/// <c>refresh</c> asks the server again. Portraits are downloaded into local files as well.
/// </summary>
public sealed class CatalogCommand(CharacterCatalog catalog, IDisplayer displayer, IPortraitProvider? portraits = null)
    : CommandBase(
        "catalog",
        "Catalog of the server's characters: catalog [list] | catalog refresh | catalog info <id|name> | catalog portraits")
{
    private const int MaxParallelDownloads = 4;

    protected override void Run(CommandContext context)
    {
        string sub = context.Args.FirstOrDefault()?.ToLowerInvariant() ?? "list";

        switch (sub)
        {
            case "list":
                List();
                break;
            case "refresh":
                RefreshAndList();
                break;
            case "info":
                Info(context.Args.ElementAtOrDefault(1));
                break;
            case "portraits":
                DownloadPortraits();
                break;
            default:
                displayer.Error($"Unknown catalog sub-command '{context.Args[0]}'. Allowed: list, refresh, info, portraits.");
                break;
        }
    }

    private void List()
    {
        var characters = EnsureCatalog();
        if (characters is not null)
            ShowTable(characters);
    }

    private void RefreshAndList()
    {
        var characters = Refresh();
        if (characters is not null)
            ShowTable(characters);
    }

    private void Info(string? idOrName)
    {
        if (string.IsNullOrWhiteSpace(idOrName))
        {
            displayer.Error("Usage: catalog info <id|name>");
            return;
        }

        var characters = EnsureCatalog();
        if (characters is null)
            return;

        var found = CharacterCatalog.Find(characters, idOrName);
        if (found is null)
        {
            displayer.Error($"Character '{idOrName}' is not in the catalog. Type 'catalog' to see all of them.");
            return;
        }

        displayer.Show(string.IsNullOrWhiteSpace(found.Title) ? $"=== {found.Name} ===" : $"=== {found.Name} \u2014 {found.Title} ===", DisplayKind.Title);
        displayer.Show($"Id: {found.Id}");
        displayer.Show($"Element: {Dash(found.Element)} | Weapon type: {Dash(found.WeaponType)} | Rarity: {found.Rarity}\u2605");
        displayer.Show(string.IsNullOrWhiteSpace(found.Description) ? "(no description)" : found.Description);

        if (found.Abilities.Count > 0)
            displayer.Show("Talents: " + string.Join(", ", found.Abilities.Select(a => a.Name)));

        displayer.Show($"Portrait link: {RemoteLinks.Portrait(found.Id)}");
        ShowLocalPortrait(found.Id);
    }

    /// <summary>Downloads the portrait (or takes it from the folder) and tells where the file lies.</summary>
    private void ShowLocalPortrait(string id)
    {
        if (portraits is null)
            return;

        byte[]? bytes = portraits.GetPortraitAsync(id).GetAwaiter().GetResult();
        string? path = bytes is null ? null : portraits.GetLocalPath(id);

        if (path is not null)
            displayer.Show($"Portrait file: {path}");
        else
            displayer.Warning("The portrait could not be downloaded (no network?). Try again later.");
    }

    private void DownloadPortraits()
    {
        if (portraits is null)
        {
            displayer.Error("Portrait download is not configured.");
            return;
        }

        IPortraitProvider provider = portraits;

        var characters = EnsureCatalog();
        if (characters is null)
            return;

        displayer.Info($"Downloading the portraits of {characters.Count} characters (those already on the disk are not downloaded again)...");

        using var gate = new SemaphoreSlim(MaxParallelDownloads);

        async Task<bool> One(string id)
        {
            await gate.WaitAsync();
            try
            {
                byte[]? bytes = await provider.GetPortraitAsync(id);
                return bytes is not null;
            }
            finally
            {
                gate.Release();
            }
        }

        bool[] results = Task.WhenAll(characters.Select(c => One(c.Id))).GetAwaiter().GetResult();
        int saved = results.Count(r => r);

        displayer.Success($"Portraits on the disk: {saved} of {characters.Count}.");
        if (saved < characters.Count)
            displayer.Warning($"{characters.Count - saved} portraits could not be downloaded; run 'catalog portraits' again later.");

        string? sample = characters.Select(c => provider.GetLocalPath(c.Id)).FirstOrDefault(p => p is not null);
        if (sample is not null)
            displayer.Info($"The files are in: {Path.GetDirectoryName(sample)}");
    }

    /// <summary>The saved catalog; when there is none yet it is downloaded first. Null when nothing can be shown.</summary>
    private IReadOnlyList<RemoteCharacterInfo>? EnsureCatalog()
    {
        IReadOnlyList<RemoteCharacterInfo> saved;
        try
        {
            saved = catalog.Cached();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            displayer.Error($"Could not read the saved catalog: {ex.Message}. Type 'catalog refresh' to download it again.");
            return null;
        }

        if (saved.Count > 0)
            return saved;

        displayer.Info("The catalog is empty, so it is downloaded from the server first.");
        return Refresh();
    }

    /// <summary>Downloads the catalog again. Null when it failed; the saved catalog is not touched then.</summary>
    private IReadOnlyList<RemoteCharacterInfo>? Refresh()
    {
        displayer.Info("Updating the catalog from the server (this can take a few seconds)...");

        CatalogRefreshResult result;
        try
        {
            result = catalog.RefreshAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or IOException
            or UnauthorizedAccessException or JsonException or NotSupportedException or TaskCanceledException)
        {
            displayer.Error($"Could not update the catalog: {ex.Message}");
            return null;
        }

        if (result.Characters.Count == 0)
        {
            displayer.Error("The server returned no characters, so the saved catalog was not changed.");
            return null;
        }

        string skipped = result.Failed > 0 ? $", {result.Failed} could not be downloaded" : string.Empty;
        displayer.Success($"Catalog updated: {result.Characters.Count} characters{skipped}.");
        return result.Characters;
    }

    private void ShowTable(IReadOnlyList<RemoteCharacterInfo> characters)
    {
        var table = new ConsoleTable("Id", "Name", "Element", "Weapon", "Rarity");
        foreach (var c in characters)
            table.AddRow(c.Id, c.Name, Dash(c.Element), Dash(c.WeaponType), $"{c.Rarity}\u2605");

        displayer.Show("=== Character catalog ===", DisplayKind.Title);
        displayer.Show(table.ToString());
        displayer.Info($"{characters.Count} characters. 'catalog info <id>' shows the details and the link to the portrait; 'remote create <id> <name>' adds one to the game.");
    }

    private static string Dash(string text) => string.IsNullOrWhiteSpace(text) ? "-" : text;
}
