using GameSystem.Core.Characters;
using GameSystem.Core.Combat.Actions;

namespace GameSystem.Core.Engine;

/// <summary>
/// Default behaviour of the simulation: when badly hurt there is a chance to heal,
/// otherwise a random choice between attack, innate ability and defend.
/// </summary>
public sealed class RandomTurnStrategy(Random? random = null) : ITurnStrategy
{
    private const double LowHealthThreshold = 0.4;
    private const int HealChancePercent = 50;

    private readonly Random _random = random ?? Random.Shared;

    public ICombatAction ChooseAction(Character actor, Character opponent)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(opponent);

        if (actor.Health < actor.MaxHealth * LowHealthThreshold && _random.Next(100) < HealChancePercent)
            return new HealAction(actor);

        return _random.Next(0, 3) switch
        {
            0 => new AttackAction(actor, opponent),
            1 => new UseAbilityAction(actor, opponent),
            _ => new DefendAction(actor)
        };
    }
}
