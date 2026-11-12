using GameSystem.Core.Characters;
using GameSystem.Core.Combat;
using GameSystem.Core.Engine;
using GameSystem.Core.Logging;
using GameSystem.Core.Management;
using GameSystem.Infra.Logging;

namespace GameSystem.ConsoleApp;

internal class Program
{
    private static void Main()
    {
        // 1. Initialize logging infrastructure and game engine
        ICombatLogger logger = new ConsoleCombatLogger();
        Game gameEngine = new(logger);

        // 2. Instantiate fighting characters through the Core factory
        Character warrior = CharacterFactory.Create("warrior", "Grosh the Mighty", logger);
        Character mage = CharacterFactory.Create("mage", "Elara the Wise", logger);

        // 3. Equip items with stats
        warrior.Equip(new Item("Flaming Sword", healthBonus: 20, attackBonus: 15));
        mage.Equip(new Item("Archmage Hat", armorBonus: 5));

        // 4. Run battle simulation directly passing the two chosen characters
        gameEngine.StartSimulation(warrior, mage);
    }
}
