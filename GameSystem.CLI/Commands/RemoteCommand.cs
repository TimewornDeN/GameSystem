using GameSystem.Core.Abstractions;
using GameSystem.Core.Combat;
using GameSystem.Core.Logging;
using GameSystem.Core.Management;
using GameSystem.Core.Models;

namespace GameSystem.CLI.Commands;

/// <summary>remote &lt;chars|weapons|create|equip&gt; — talks to the remote character database.</summary>
public sealed class RemoteCommand(
    GameWorld world,
    IDisplayer displayer,
    ICombatLogger logger,
    IGenshinApiClient? apiClient = null,
    Random? random = null)
    : CommandBase(
        "remote",
        "Talks to the remote character DB: remote chars | remote weapons | remote create <id> <n> | remote equip <char_name> <weapon_id>")
{
    private readonly Random _random = random ?? new Random();

    protected override void Run(CommandContext context)
    {
        if (context.Args.Count == 0)
        {
            displayer.Error("Usage: remote <chars|weapons|create|equip> ...");
            return;
        }

        switch (context.Args[0].ToLowerInvariant())
        {
            case "chars":
                ListCharacters();
                break;
            case "weapons":
                ListWeapons();
                break;
            case "create":
                CreateCharacter(context.Args.ElementAtOrDefault(1), context.Args.ElementAtOrDefault(2));
                break;
            case "equip":
                EquipWeapon(context.Args.ElementAtOrDefault(1), context.Args.ElementAtOrDefault(2));
                break;
            default:
                displayer.Error($"Unknown remote sub-command '{context.Args[0]}'. Allowed: chars, weapons, create, equip.");
                break;
        }
    }

    private void ListCharacters()
    {
        if (!TryGetApiClient(out var client)) return;

        try
        {
            var ids = client.GetCharacterIdsAsync().GetAwaiter().GetResult();
            displayer.Show("=== Remote Characters ===", DisplayKind.Title);
            foreach (var id in ids) displayer.Show($"- {id}");
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            displayer.Error($"Could not reach remote character database: {ex.Message}");
        }
    }

    private void ListWeapons()
    {
        if (!TryGetApiClient(out var client)) return;

        try
        {
            var ids = client.GetWeaponIdsAsync().GetAwaiter().GetResult();
            displayer.Show("=== Remote Weapons ===", DisplayKind.Title);
            foreach (var id in ids) displayer.Show($"- {id}");
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            displayer.Error($"Could not reach remote weapon database: {ex.Message}");
        }
    }

    /// <summary>
    /// remote create &lt;remote_id&gt; &lt;display_name&gt; — fetches a character from the remote database and creates a local
    /// Warrior/Mage from it: weapon type decides the class, rarity decides the starter stats (via a generated item)
    /// and up to two of its talents become learnable abilities.
    /// </summary>
    private void CreateCharacter(string? remoteId, string? displayName)
    {
        if (!TryGetApiClient(out var client)) return;

        if (string.IsNullOrWhiteSpace(remoteId) || string.IsNullOrWhiteSpace(displayName))
        {
            displayer.Error("Usage: remote create <remote_id> <display_name>");
            return;
        }

        if (world.FindCharacter(displayName) is not null)
        {
            displayer.Error($"A character named '{displayName}' already exists.");
            return;
        }

        RemoteCharacterInfo info;
        try
        {
            info = client.GetCharacterAsync(remoteId).GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            displayer.Error($"Could not fetch character '{remoteId}': {ex.Message}");
            return;
        }

        string classId = info.WeaponType.Equals("Catalyst", StringComparison.OrdinalIgnoreCase) ? "mage" : "warrior";
        var character = CharacterFactory.Create(classId, displayName, logger);

        character.Equip(RemoteStatGenerator.GenerateRarityBonusItem(info.Rarity, _random));

        foreach (var ability in RemoteStatGenerator.GenerateAbilities(info.Abilities, info.Rarity))
        {
            try { character.Learn(ability); }
            catch (InvalidOperationException) { /* id collided with the innate ability; skip it */ }
        }

        world.Characters.Add(character);
        world.RemoteInfo[character.Name] = info;

        displayer.Success($"Created {classId} '{displayName}' from remote character '{info.Name}' ({info.Rarity}\u2605, {info.Element}).");
    }

    private void EquipWeapon(string? charName, string? weaponId)
    {
        if (!TryGetApiClient(out var client)) return;

        if (string.IsNullOrWhiteSpace(charName) || string.IsNullOrWhiteSpace(weaponId))
        {
            displayer.Error("Usage: remote equip <char_name> <weapon_id>");
            return;
        }

        var character = world.FindCharacter(charName);
        if (character is null)
        {
            displayer.Error($"Character '{charName}' not found.");
            return;
        }

        RemoteWeaponInfo weapon;
        try
        {
            weapon = client.GetWeaponAsync(weaponId).GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            displayer.Error($"Could not fetch weapon '{weaponId}': {ex.Message}");
            return;
        }

        character.Equip(new Item(weapon.Name, attackBonus: Math.Max(0, weapon.BaseAttack)));
        displayer.Success($"Equipped '{weapon.Name}' ({weapon.Type}, {weapon.Rarity}\u2605) on character '{character.Name}'.");
    }

    private bool TryGetApiClient(out IGenshinApiClient client)
    {
        if (apiClient is not null)
        {
            client = apiClient;
            return true;
        }

        displayer.Error("Remote character database is not configured.");
        client = null!;
        return false;
    }
}
