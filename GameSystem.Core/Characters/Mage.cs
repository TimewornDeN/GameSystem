using GameSystem.Core.Combat;
using GameSystem.Core.Combat.Strategies;
using GameSystem.Core.Logging;

namespace GameSystem.Core.Characters;

/// <summary>
/// Represents a spellcaster with low physical resistance but high armor-piercing damage.
/// </summary>
/// <param name="name">Mage character name.</param>
/// <param name="logger">Logger service instance.</param>
public class Mage(string name, ICombatLogger logger)
    : Character(name, maxHealth: 80, armor: 2, attackPower: 15, logger)
{
    public override string ClassId => "mage";

    /// <summary>Fireball: deals fixed damage that ignores armor and 20% of resist, and lowers the target's resist for two turns.</summary>
    protected override Ability CreateDefaultAbility()
        => new(
            "fireball",
            "Fireball",
            new FixedDamageStrategy(35, ignoresArmor: true),
            effect: new StatusEffect("Scorched", StatKind.Resist, -10, Turns: 2),
            resistPenetration: 20);
}
