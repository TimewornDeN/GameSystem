using GameSystem.Core.Combat;
using GameSystem.Core.Combat.Strategies;
using GameSystem.Core.Logging;

namespace GameSystem.Core.Characters;

/// <summary>
/// Represents a resilient warrior with high health and physical strike capabilities.
/// </summary>
/// <param name="name">Warrior character name.</param>
/// <param name="logger">Logger service instance.</param>
public class Warrior(string name, ICombatLogger logger)
    : Character(name, maxHealth: 150, armor: 10, attackPower: 20, logger)
{
    public override string ClassId => "warrior";

    /// <summary>Heavy Strike: deals double the warrior's current attack power and breaks the target's armor for two turns.</summary>
    protected override Ability CreateDefaultAbility()
        => new(
            "heavy-strike",
            "Heavy Strike",
            new AttackScalingDamageStrategy(multiplier: 2),
            effect: new StatusEffect("Armor Break", StatKind.Armor, -3, Turns: 2));
}
