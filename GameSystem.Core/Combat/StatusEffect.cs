namespace GameSystem.Core.Combat;

/// <summary>The characteristics that a buff or a debuff can change.</summary>
public enum StatKind
{
    Attack,
    Armor,
    Resist,
    Speed
}

/// <summary>
/// A temporary modifier of one characteristic: positive <paramref name="Amount"/> is a buff, negative is a debuff.
/// It lasts for <paramref name="Turns"/> more turns (rounds) and is immutable: ticking produces a new instance.
/// </summary>
public sealed record StatusEffect(string Name, StatKind Stat, int Amount, int Turns)
{
    public bool IsDebuff => Amount < 0;

    /// <summary>The same effect with one turn less left.</summary>
    public StatusEffect Tick() => this with { Turns = Turns - 1 };

    public override string ToString() => $"{Name} ({Stat} {Amount:+#;-#;0}, {Turns} turn(s))";
}
