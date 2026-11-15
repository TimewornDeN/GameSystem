using GameSystem.Core.Combat;
using GameSystem.Core.Logging;

namespace GameSystem.Core.Characters;

/// <summary>
/// Base abstract class for all game characters. Encapsulates stats, health, armor, and combat logic.
/// Contains no console output: all narration goes through the injected ICombatLogger.
/// </summary>
public abstract class Character
{
    /// <summary> Gets the character's name. </summary>
    public string Name { get; }

    /// <summary> Gets the current health points of the character. </summary>
    public int Health { get; protected set; }

    /// <summary> Gets the maximum health capacity of the character. </summary>
    public int MaxHealth { get; protected set; }

    /// <summary> Gets the total armor rating of the character. </summary>
    public int Armor { get; protected set; }

    /// <summary> Gets the base attack damage power. </summary>
    public int AttackPower { get; protected set; }

    /// <summary> Speed of a character without any bonuses; the turn order is based on it. </summary>
    public const int BaseSpeed = 100;

    /// <summary> Percentage (0-75) of every hit that is ignored after the armor was subtracted. </summary>
    public int Resist { get; private set; }

    /// <summary> Gets the base speed (including the equipped item, without buffs). </summary>
    public int Speed { get; private set; } = BaseSpeed;

    /// <summary> Gets the currently equipped item, if any. </summary>
    public Item? EquippedItem { get; private set; }

    /// <summary> Gets a value indicating whether the character is currently in a defensive stance. </summary>
    public bool IsDefending { get; private set; }

    /// <summary> Short, stable identifier of the character's class (used by persistence and factories). </summary>
    public abstract string ClassId { get; }

    /// <summary> Id of the innate ability every character of this class is born with. </summary>
    public string DefaultAbilityId { get; }

    /// <summary> Logger service used to emit character-related events. </summary>
    protected readonly ICombatLogger Logger;

    private readonly List<Ability> _abilities = [];
    private readonly List<StatusEffect> _effects = [];

    /// <summary> Buffs and debuffs that are active right now. </summary>
    public IReadOnlyList<StatusEffect> ActiveEffects => _effects;

    /// <summary> Attack power with buffs and debuffs. This is what attacks and attack-scaling abilities use. </summary>
    public int EffectiveAttack => Math.Max(1, AttackPower + EffectBonus(StatKind.Attack));

    /// <summary> Armor with buffs and debuffs, doubled while the character is defending. </summary>
    public int EffectiveArmor => Math.Max(0, Armor + EffectBonus(StatKind.Armor)) * (IsDefending ? 2 : 1);

    /// <summary> Resist with buffs and debuffs, kept inside the allowed range. </summary>
    public int EffectiveResist => Math.Clamp(Resist + EffectBonus(StatKind.Resist), 0, DamageCalculator.MaxResist);

    /// <summary> Speed with buffs and debuffs (never below 1). </summary>
    public int EffectiveSpeed => Math.Max(1, Speed + EffectBonus(StatKind.Speed));

    private int EffectBonus(StatKind stat) => _effects.Where(e => e.Stat == stat).Sum(e => e.Amount);

    /// <summary> Every ability this character currently knows (innate plus learned). </summary>
    public IReadOnlyList<Ability> Abilities => _abilities;

    /// <summary>
    /// Initializes base attributes of a new character instance.
    /// </summary>
    /// <param name="name">The character's display name.</param>
    /// <param name="maxHealth">Maximum starting health.</param>
    /// <param name="armor">Initial armor rating.</param>
    /// <param name="attackPower">Initial attack power.</param>
    /// <param name="logger">Logger service instance for event messaging.</param>
    protected Character(string name, int maxHealth, int armor, int attackPower, ICombatLogger logger)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Character name cannot be null or whitespace.", nameof(name));

        if (maxHealth <= 0)
            throw new ArgumentException("Maximum health must be a positive integer.", nameof(maxHealth));

        if (armor < 0)
            throw new ArgumentException("Armor cannot be negative.", nameof(armor));

        if (attackPower <= 0)
            throw new ArgumentException("Attack power must be a positive integer.", nameof(attackPower));

        ArgumentNullException.ThrowIfNull(logger);

        Name = name;
        MaxHealth = maxHealth;
        Health = maxHealth;
        Armor = armor;
        AttackPower = attackPower;
        Logger = logger;

        Ability defaultAbility = CreateDefaultAbility();
        DefaultAbilityId = defaultAbility.Id;
        _abilities.Add(defaultAbility);
    }

    /// <summary>Builds the class-specific innate ability granted to every character of this class.</summary>
    protected abstract Ability CreateDefaultAbility();

    /// <summary>
    /// Performs a basic attack against a target character.
    /// </summary>
    /// <param name="target">The target receiving the attack.</param>
    public virtual void Attack(Character target)
    {
        ArgumentNullException.ThrowIfNull(target);

        Logger.Log($"[{Name}] attacks [{target.Name}] with {EffectiveAttack} power!", LogColor.White);
        target.TakeDamage(EffectiveAttack);
    }

    /// <summary>
    /// Receives a plain physical blow of the given strength (armor applies, nothing is ignored).
    /// </summary>
    /// <param name="damage">Raw incoming damage value.</param>
    public virtual void TakeDamage(int damage)
    {
        if (damage < 0)
            throw new ArgumentException("Damage cannot be negative.", nameof(damage));

        TakeHit(new Hit(damage));
    }

    /// <summary>
    /// Receives a blow and loses health according to the damage formula of <see cref="DamageCalculator"/>:
    /// flat armor first, then the percentage resist.
    /// </summary>
    public void TakeHit(Hit hit)
    {
        int armor = hit.IgnoresArmor ? 0 : EffectiveArmor;
        int actualDamage = DamageCalculator.Resolve(hit, EffectiveArmor, EffectiveResist);

        Health = Math.Max(0, Health - actualDamage);

        string resistNote = EffectiveResist > 0 ? $", Resist: {EffectiveResist}%" : string.Empty;
        Logger.Log($"  -> [{Name}] takes {actualDamage} damage (Armor: {armor}{resistNote}). Health left: {Health}/{MaxHealth}", LogColor.Red);
        IsDefending = false;
    }

    /// <summary>
    /// Puts a buff or a debuff on the character. An effect with the same name is refreshed instead of stacked.
    /// </summary>
    public void AddEffect(StatusEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (effect.Turns <= 0)
            throw new ArgumentException("An effect must last at least one turn.", nameof(effect));

        int index = _effects.FindIndex(e => e.Name.Equals(effect.Name, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            _effects[index] = effect;
        else
            _effects.Add(effect);

        Logger.Log($"  -> [{Name}] is affected by {effect}.", effect.IsDebuff ? LogColor.Magenta : LogColor.Cyan);
    }

    /// <summary>One turn passes: every effect has one turn less left and expired effects are removed.</summary>
    public void TickEffects()
    {
        for (int i = _effects.Count - 1; i >= 0; i--)
        {
            var ticked = _effects[i].Tick();
            if (ticked.Turns <= 0)
            {
                Logger.Log($"  -> '{ticked.Name}' wears off from [{Name}].", LogColor.Gray);
                _effects.RemoveAt(i);
            }
            else
            {
                _effects[i] = ticked;
            }
        }
    }

    /// <summary>Removes all buffs and debuffs without narration (used when a new battle starts).</summary>
    public void ClearEffects() => _effects.Clear();

    /// <summary>
    /// Raises defensive stance to double armor on the next incoming hit.
    /// </summary>
    public void Defend()
    {
        IsDefending = true;
        Logger.Log($"[{Name}] assumes a defensive stance! Armor temporarily doubled.", LogColor.Cyan);
    }

    /// <summary>
    /// Restores a given amount of health to the character up to MaxHealth.
    /// </summary>
    /// <param name="amount">Amount of health points to heal.</param>
    public void Heal(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Heal amount must be a positive integer.", nameof(amount));

        Health = Math.Min(MaxHealth, Health + amount);
        Logger.Log($"[{Name}] heals for {amount} HP. Current Health: {Health}/{MaxHealth}", LogColor.Green);
    }

    /// <summary>
    /// Equips an item and applies stat bonuses to the character, replacing any previously equipped item.
    /// </summary>
    /// <param name="item">The item to equip.</param>
    public void Equip(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        DetachCurrentItem();
        AttachItem(item);

        Logger.Log($"[{Name}] equipped '{item.Name}'. Stats updated!", LogColor.DarkYellow);
    }

    /// <summary>Removes the bonuses of the equipped item (if any) without producing narration.</summary>
    private void DetachCurrentItem()
    {
        if (EquippedItem is null) return;

        MaxHealth -= EquippedItem.HealthBonus;
        Armor -= EquippedItem.ArmorBonus;
        AttackPower -= EquippedItem.AttackBonus;
        Resist -= EquippedItem.ResistBonus;
        Speed -= EquippedItem.SpeedBonus;
        Health = Math.Min(Health, MaxHealth);
        EquippedItem = null;
    }

    /// <summary>Applies the bonuses of an item and marks it as equipped, without producing narration.</summary>
    private void AttachItem(Item item)
    {
        EquippedItem = item;
        MaxHealth += item.HealthBonus;
        Health += item.HealthBonus;
        Armor += item.ArmorBonus;
        AttackPower += item.AttackBonus;
        Resist += item.ResistBonus;
        Speed += item.SpeedBonus;
    }

    /// <summary>Adds a custom ability to the character's known abilities.</summary>
    public void Learn(Ability ability)
    {
        ArgumentNullException.ThrowIfNull(ability);

        if (_abilities.Any(a => a.Id.Equals(ability.Id, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Character '{Name}' already knows ability '{ability.Id}'.");

        _abilities.Add(ability);
    }

    /// <summary>Uses the character's innate (default) ability against a target.</summary>
    public void UseAbility(Character target) => UseAbility(DefaultAbilityId, target);

    /// <summary>Uses a specific known ability (innate or learned) against a target.</summary>
    public void UseAbility(string abilityId, Character target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (string.IsNullOrWhiteSpace(abilityId))
            throw new ArgumentException("Ability id cannot be empty.", nameof(abilityId));

        var ability = _abilities.FirstOrDefault(a => a.Id.Equals(abilityId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Character '{Name}' does not know ability '{abilityId}'.");

        Logger.Log($"\n[ABILITY] {Name} uses '{ability.Description}' on {target.Name}!", LogColor.Magenta);
        target.TakeHit(ability.CreateHit(this, target));

        if (ability.Effect is { } effect && (ability.EffectOnSelf || target.Health > 0))
            (ability.EffectOnSelf ? this : target).AddEffect(effect);
    }

    /// <summary>Directly sets current health, clamped to [0, MaxHealth]; used when restoring persisted state.</summary>
    public void SetHealth(int health) => Health = Math.Clamp(health, 0, MaxHealth);

    // ---------------- MEMENTO (originator side) ----------------

    /// <summary>
    /// MEMENTO. Captures an immutable snapshot of everything that can change during play: health,
    /// equipment, learned abilities and stance. Stats derived from equipment are NOT stored; they are
    /// recomputed when the snapshot is applied, so a snapshot can never put the character into an inconsistent state.
    /// </summary>
    public CharacterMemento CreateMemento() => new(
        ClassId,
        Name,
        Health,
        EquippedItem,
        [.. _abilities.Where(a => !a.Id.Equals(DefaultAbilityId, StringComparison.Ordinal))],
        IsDefending,
        [.. _effects]);

    /// <summary>Rolls this character back to the state captured in <paramref name="memento"/>. Produces no narration.</summary>
    /// <exception cref="ArgumentException">The memento was taken from a different character.</exception>
    public void Restore(CharacterMemento memento)
    {
        ArgumentNullException.ThrowIfNull(memento);

        if (!memento.ClassId.Equals(ClassId, StringComparison.Ordinal) || !memento.Name.Equals(Name, StringComparison.Ordinal))
            throw new ArgumentException(
                $"Memento of '{memento.Name}' ({memento.ClassId}) cannot be applied to '{Name}' ({ClassId}).",
                nameof(memento));

        DetachCurrentItem();
        if (memento.EquippedItem is not null)
            AttachItem(memento.EquippedItem);

        _abilities.RemoveAll(a => !a.Id.Equals(DefaultAbilityId, StringComparison.Ordinal));
        _abilities.AddRange(memento.LearnedAbilities);

        IsDefending = memento.IsDefending;

        _effects.Clear();
        if (memento.ActiveEffects is not null)
            _effects.AddRange(memento.ActiveEffects);
        SetHealth(memento.Health); // After equipment, so MaxHealth is already final.
    }
}
