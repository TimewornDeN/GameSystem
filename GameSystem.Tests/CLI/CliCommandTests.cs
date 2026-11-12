using GameSystem.CLI;
using GameSystem.Core.Models;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.CLI;

public class CliCommandTests
{
    private static (Cli Cli, FakeDisplayer Displayer, FakeInputReader Input, InMemoryGameStateRepository Repository)
        CreateCli(params string?[] inputAnswers)
    {
        var displayer = new FakeDisplayer();
        var input = new FakeInputReader(inputAnswers);
        var logger = new FakeCombatLogger();
        var repository = new InMemoryGameStateRepository();
        var cli = CliFactory.Create(displayer, input, logger, repository, new InMemoryGameResultRepository());
        return (cli, displayer, input, repository);
    }

    private static (Cli Cli, FakeDisplayer Displayer, FakeGenshinApiClient ApiClient)
        CreateCliWithApi(params string?[] inputAnswers)
    {
        var displayer = new FakeDisplayer();
        var input = new FakeInputReader(inputAnswers);
        var logger = new FakeCombatLogger();
        var repository = new InMemoryGameStateRepository();
        var apiClient = new FakeGenshinApiClient();
        var cli = CliFactory.Create(displayer, input, logger, repository, new InMemoryGameResultRepository(), apiClient);
        return (cli, displayer, apiClient);
    }

    // ---------------- create ----------------

    [Fact]
    public void CreateChar_Succeeds_AndReportsClassAndName()
    {
        var (cli, displayer, _, _) = CreateCli("Grosh", "1");

        cli.ExecCommand("create char");

        Assert.True(displayer.HasSuccess("Created warrior 'Grosh'"));
    }

    [Fact]
    public void CreateChar_DuplicateName_ReportsError_WithoutAskingForClassAgain()
    {
        var (cli, displayer, _, _) = CreateCli("Grosh", "1", "Grosh");

        cli.ExecCommand("create char");
        cli.ExecCommand("create char");

        Assert.True(displayer.HasError("already exists"));
    }

    [Fact]
    public void CreateChar_InvalidClassChoice_ReprompsUntilValid()
    {
        var (cli, displayer, _, _) = CreateCli("Grosh", "9", "2");

        cli.ExecCommand("create char");

        Assert.Contains(displayer.Entries, e => e.Text.Contains("Invalid choice"));
        Assert.True(displayer.HasSuccess("Created mage 'Grosh'"));
    }

    [Fact]
    public void CreateItem_Succeeds()
    {
        var (cli, displayer, _, _) = CreateCli("Flaming Sword");

        cli.ExecCommand("create item");

        Assert.True(displayer.HasSuccess("Created item 'Flaming Sword'"));
    }

    [Fact]
    public void CreateItem_DuplicateName_ReportsError()
    {
        var (cli, displayer, _, _) = CreateCli("Sword", "Sword");

        cli.ExecCommand("create item");
        cli.ExecCommand("create item");

        Assert.True(displayer.HasError("already exists"));
    }

    [Fact]
    public void CreateAbility_Succeeds_WithGivenPower()
    {
        var (cli, displayer, _, _) = CreateCli("shield-bash", "Shield Bash", "25");

        cli.ExecCommand("create ability");

        Assert.True(displayer.HasSuccess("power 25"));
    }

    [Fact]
    public void CreateAbility_InvalidPower_ReprompsUntilValid()
    {
        var (cli, displayer, _, _) = CreateCli("shield-bash", "Shield Bash", "abc", "10");

        cli.ExecCommand("create ability");

        Assert.Contains(displayer.Entries, e => e.Text.Contains("Invalid number"));
        Assert.True(displayer.HasSuccess("power 10"));
    }

    [Fact]
    public void CreateObject_UnknownType_ReportsError()
    {
        var (cli, displayer, _, _) = CreateCli();

        cli.ExecCommand("create potion");

        Assert.True(displayer.HasError("Unknown type"));
    }

    // ---------------- add ----------------

    [Fact]
    public void AddToCharacter_EquipsExistingItem()
    {
        var (cli, displayer, _, _) = CreateCli("Grosh", "1", "Sword");
        cli.ExecCommand("create char");
        cli.ExecCommand("create item");

        cli.ExecCommand("add --char_id Grosh --id Sword");

        Assert.True(displayer.HasSuccess("Equipped item 'Sword'"));
    }

    [Fact]
    public void AddToCharacter_TeachesExistingAbility()
    {
        var (cli, displayer, _, _) = CreateCli("Grosh", "1", "shield-bash", "Shield Bash", "25");
        cli.ExecCommand("create char");
        cli.ExecCommand("create ability");

        cli.ExecCommand("add --char_id Grosh --id shield-bash");

        Assert.True(displayer.HasSuccess("Taught ability 'shield-bash'"));
    }

    [Fact]
    public void AddToCharacter_UnknownTarget_ReportsError()
    {
        var (cli, displayer, _, _) = CreateCli("Grosh", "1");
        cli.ExecCommand("create char");

        cli.ExecCommand("add --char_id Grosh --id does-not-exist");

        Assert.True(displayer.HasError("Neither item nor ability"));
    }

    [Fact]
    public void AddToCharacter_UnknownCharacter_ReportsError()
    {
        var (cli, displayer, _, _) = CreateCli("Sword");
        cli.ExecCommand("create item");

        cli.ExecCommand("add --char_id Nobody --id Sword");

        Assert.True(displayer.HasError("Character 'Nobody' not found"));
    }

    [Fact]
    public void AddToCharacter_MissingOptions_ReportsUsage()
    {
        var (cli, displayer, _, _) = CreateCli();
        cli.ExecCommand("add");

        Assert.True(displayer.HasError("Usage: add"));
    }

    // ---------------- act ----------------

    [Fact]
    public void PerformAction_Attack_DealsDamageToTarget()
    {
        var (cli, displayer, _, _) = CreateCli("Grosh", "1", "Elara", "2");
        cli.ExecCommand("create char"); // Grosh, warrior, AttackPower 20
        cli.ExecCommand("create char"); // Elara, mage, Armor 2

        cli.ExecCommand("act attack Grosh Elara");
        cli.ExecCommand("ls char --id Elara");

        Assert.True(displayer.Any("HP: 62/80")); // 80 - (20 - 2)
    }

    [Fact]
    public void PerformAction_Heal_RestoresTargetHealth_UpToMax()
    {
        var (cli, displayer, _, _) = CreateCli("Grosh", "1", "Elara", "2");
        cli.ExecCommand("create char");
        cli.ExecCommand("create char");

        cli.ExecCommand("act attack Grosh Elara"); // Elara: 80 -> 62
        cli.ExecCommand("act heal Grosh Elara");   // Elara: 62 -> 82 -> capped at 80
        cli.ExecCommand("ls char --id Elara");

        Assert.True(displayer.Any("HP: 80/80"));
    }

    [Fact]
    public void PerformAction_Defend_DoesNotRequireTarget_AndSucceeds()
    {
        var (cli, displayer, _, _) = CreateCli("Grosh", "1");
        cli.ExecCommand("create char");

        cli.ExecCommand("act defend Grosh");

        Assert.Empty(displayer.Errors);
    }

    [Fact]
    public void PerformAction_Ability_Default_UsesInnateAbility()
    {
        var (cli, displayer, _, _) = CreateCli("Grosh", "1", "Elara", "2");
        cli.ExecCommand("create char"); // warrior, AttackPower 20 -> heavy-strike power 40
        cli.ExecCommand("create char"); // mage, Armor 2

        cli.ExecCommand("act ability Grosh Elara");
        cli.ExecCommand("ls char --id Elara");

        Assert.True(displayer.Any("HP: 42/80")); // 80 - (40 - 2)
    }

    [Fact]
    public void PerformAction_Ability_ById_UsesLearnedAbility()
    {
        var (cli, displayer, _, _) = CreateCli(
            "Grosh", "1", "Elara", "2", "shield-bash", "Shield Bash", "25");
        cli.ExecCommand("create char");
        cli.ExecCommand("create char");
        cli.ExecCommand("create ability");
        cli.ExecCommand("add --char_id Grosh --id shield-bash");

        cli.ExecCommand("act ability Grosh Elara --id shield-bash");
        cli.ExecCommand("ls char --id Elara");

        Assert.True(displayer.Any("HP: 57/80")); // 80 - (25 - 2)
    }

    [Fact]
    public void PerformAction_UnknownAbilityId_ReportsErrorInsteadOfThrowing()
    {
        var (cli, displayer, _, _) = CreateCli("Grosh", "1", "Elara", "2");
        cli.ExecCommand("create char");
        cli.ExecCommand("create char");

        cli.ExecCommand("act ability Grosh Elara --id does-not-exist");

        Assert.True(displayer.HasError("does not know ability"));
    }

    [Fact]
    public void PerformAction_UnknownActor_ReportsError()
    {
        var (cli, displayer, _, _) = CreateCli();

        cli.ExecCommand("act attack Nobody AlsoNobody");

        Assert.True(displayer.HasError("Actor character 'Nobody' not found"));
    }

    [Fact]
    public void PerformAction_MissingTarget_ForNonDefendAction_ReportsError()
    {
        var (cli, displayer, _, _) = CreateCli("Grosh", "1");
        cli.ExecCommand("create char");

        cli.ExecCommand("act attack Grosh");

        Assert.True(displayer.HasError("requires a target"));
    }

    // ---------------- ls ----------------

    [Fact]
    public void ListObjects_NoCharactersYet_ShowsWarning()
    {
        var (cli, displayer, _, _) = CreateCli();

        cli.ExecCommand("ls char");

        Assert.Contains(displayer.Entries, e => e.Text.Contains("No characters found"));
    }

    [Fact]
    public void ListObjects_UnknownCategory_ReportsError()
    {
        var (cli, displayer, _, _) = CreateCli();

        cli.ExecCommand("ls potion");

        Assert.True(displayer.HasError("Unknown category"));
    }

    // ---------------- save / load / list ----------------

    [Fact]
    public void Save_WithoutName_ReportsUsage()
    {
        var (cli, displayer, _, _) = CreateCli();

        cli.ExecCommand("save");

        Assert.True(displayer.HasError("Usage: save"));
    }

    [Fact]
    public void Save_PersistsStateInRepository()
    {
        var (cli, displayer, _, repository) = CreateCli("Grosh", "1");
        cli.ExecCommand("create char");

        cli.ExecCommand("save battle-1");

        Assert.True(repository.Exists("battle-1"));
        Assert.True(displayer.HasSuccess("saved as 'battle-1'"));
    }

    [Fact]
    public void Load_UnknownSave_ReportsError()
    {
        var (cli, displayer, _, _) = CreateCli();

        cli.ExecCommand("load missing");

        Assert.True(displayer.HasError("not found"));
    }

    [Fact]
    public void Load_RestoresCharacterEquipmentAndAbilities()
    {
        var (cli, _, _, repository) = CreateCli(
            "Grosh", "1", "Sword", "shield-bash", "Shield Bash", "25");
        cli.ExecCommand("create char");
        cli.ExecCommand("create item");
        cli.ExecCommand("create ability");
        cli.ExecCommand("add --char_id Grosh --id Sword");
        cli.ExecCommand("add --char_id Grosh --id shield-bash");
        cli.ExecCommand("act attack Grosh Grosh"); // self-hit just to move HP off max
        cli.ExecCommand("save snapshot");

        // Fresh CLI sharing the same (in-memory) repository, simulating a new session.
        var freshDisplayer = new FakeDisplayer();
        var freshCli = CliFactory.Create(freshDisplayer, new FakeInputReader(), new FakeCombatLogger(), repository, new InMemoryGameResultRepository());

        freshCli.ExecCommand("load snapshot");
        freshCli.ExecCommand("ls char --id Grosh");

        Assert.True(freshDisplayer.Any("Item: Sword"));
        Assert.True(freshDisplayer.Any("shield-bash"));
    }

    [Fact]
    public void List_WithNoSaves_ShowsInfoMessage()
    {
        var (cli, displayer, _, _) = CreateCli();

        cli.ExecCommand("list");

        Assert.Contains(displayer.Entries, e => e.Text.Contains("No saved games"));
    }

    [Fact]
    public void List_AfterSaving_ShowsSaveName()
    {
        var (cli, displayer, _, _) = CreateCli("Grosh", "1");
        cli.ExecCommand("create char");
        cli.ExecCommand("save battle-1");

        cli.ExecCommand("list");

        Assert.True(displayer.Any("battle-1"));
    }

    // ---------------- remote (WebApi) ----------------

    private static RemoteCharacterInfo SampleRemoteCharacter() => new(
        Id: "albedo",
        Name: "Albedo",
        Title: "Kreideprinz",
        Element: "Geo",
        WeaponType: "Sword",
        Rarity: 5,
        Description: "The Chief Alchemist of the Knights of Favonius.",
        Abilities:
        [
            new RemoteAbilityInfo("Favonius Bladework", "Performs up to 5 strikes."),
            new RemoteAbilityInfo("Abiogenesis: Solar Isotoma", "Creates a Fatal Blossom field.")
        ]);

    private static RemoteWeaponInfo SampleRemoteWeapon() => new(
        Id: "skyward-blade", Name: "Skyward Blade", Type: "Sword", Rarity: 5,
        BaseAttack: 44, PassiveName: "Sky-Piercing Fang", PassiveDescription: "CRIT Rate +4%.",
        Description: "The sword of a knight.");

    [Fact]
    public void RemoteListCharacters_WithoutApiClient_ReportsError()
    {
        var (cli, displayer, _, _) = CreateCli();

        cli.ExecCommand("remote chars");

        Assert.True(displayer.HasError("not configured"));
    }

    [Fact]
    public void RemoteListCharacters_ListsIdsFromApi()
    {
        var (cli, displayer, api) = CreateCliWithApi();
        api.AddCharacter(SampleRemoteCharacter());

        cli.ExecCommand("remote chars");

        Assert.True(displayer.Any("albedo"));
    }

    [Fact]
    public void RemoteCreateCharacter_CreatesWarriorForSwordUser_WithGearAndAbilities()
    {
        var (cli, displayer, api) = CreateCliWithApi();
        api.AddCharacter(SampleRemoteCharacter());

        cli.ExecCommand("remote create albedo MyAlbedo");

        Assert.True(displayer.HasSuccess("Created warrior 'MyAlbedo'"));
    }

    [Fact]
    public void RemoteCreateCharacter_CatalystUser_BecomesMage()
    {
        var (cli, displayer, api) = CreateCliWithApi();
        var mageInfo = SampleRemoteCharacter() with { Id = "klee", Name = "Klee", WeaponType = "Catalyst" };
        api.AddCharacter(mageInfo);

        cli.ExecCommand("remote create klee MyKlee");

        Assert.True(displayer.HasSuccess("Created mage 'MyKlee'"));
    }

    [Fact]
    public void RemoteCreateCharacter_UnknownRemoteId_ReportsError()
    {
        var (cli, displayer, _) = CreateCliWithApi();

        cli.ExecCommand("remote create does-not-exist Whoever");

        Assert.True(displayer.HasError("Could not fetch character"));
    }

    [Fact]
    public void RemoteCreateCharacter_DuplicateDisplayName_ReportsError()
    {
        var (cli, displayer, api) = CreateCliWithApi();
        api.AddCharacter(SampleRemoteCharacter());

        cli.ExecCommand("remote create albedo Dup");
        cli.ExecCommand("remote create albedo Dup");

        Assert.True(displayer.HasError("already exists"));
    }

    [Fact]
    public void RemoteEquipWeapon_EquipsFetchedWeapon()
    {
        var (cli, displayer, api) = CreateCliWithApi();
        api.AddCharacter(SampleRemoteCharacter());
        api.AddWeapon(SampleRemoteWeapon());
        cli.ExecCommand("remote create albedo MyAlbedo");

        cli.ExecCommand("remote equip MyAlbedo skyward-blade");

        Assert.True(displayer.HasSuccess("Equipped 'Skyward Blade'"));
    }

    [Fact]
    public void RemoteEquipWeapon_UnknownCharacter_ReportsError()
    {
        var (cli, displayer, api) = CreateCliWithApi();
        api.AddWeapon(SampleRemoteWeapon());

        cli.ExecCommand("remote equip Nobody skyward-blade");

        Assert.True(displayer.HasError("Character 'Nobody' not found"));
    }

    // ---------------- sheet (structured text) ----------------

    [Fact]
    public void ShowSheet_UnknownCharacter_ReportsError()
    {
        var (cli, displayer, _, _) = CreateCli();

        cli.ExecCommand("sheet Nobody");

        Assert.True(displayer.HasError("not found"));
    }

    [Fact]
    public void ShowSheet_LocalCharacter_RendersNameAndStats()
    {
        var (cli, displayer, _, _) = CreateCli("Grosh", "1");
        cli.ExecCommand("create char");

        cli.ExecCommand("sheet Grosh");

        Assert.True(displayer.Any("Grosh"));
        Assert.True(displayer.Any("HP: 150/150"));
    }

    [Fact]
    public void ShowSheet_RemoteCharacter_IncludesTitleAndElement()
    {
        var (cli, displayer, api) = CreateCliWithApi();
        api.AddCharacter(SampleRemoteCharacter());
        cli.ExecCommand("remote create albedo MyAlbedo");

        cli.ExecCommand("sheet MyAlbedo");

        Assert.True(displayer.Any("Kreideprinz"));
        Assert.True(displayer.Any("Element: Geo"));
    }
}
