namespace GameSystem.Core.Combat;

/// <summary>
/// One incoming blow before the target's defences are applied.
/// </summary>
/// <param name="Amount">Raw damage (the "dmg_raw" of the design document).</param>
/// <param name="IgnoresArmor">Magic-like damage: flat armor does not reduce it.</param>
/// <param name="ResistPenetration">Percentage points of the target's resist that this hit ignores.</param>
public readonly record struct Hit(int Amount, bool IgnoresArmor = false, int ResistPenetration = 0);
