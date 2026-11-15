using GameSystem.Core.Models;

namespace GameSystem.Core.Combat;

/// <summary>Represents an equippable item with immutable stat modifiers, built from a pure ItemModel.</summary>
public class Item
{
    public ItemModel Model { get; }

    public string Name => Model.Name;
    public int HealthBonus => Model.HealthBonus;
    public int ArmorBonus => Model.ArmorBonus;
    public int AttackBonus => Model.AttackBonus;
    public int ResistBonus => Model.ResistBonus;
    public int SpeedBonus => Model.SpeedBonus;

    public Item(ItemModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(model.Name))
            throw new ArgumentException("Item name cannot be null or whitespace.", nameof(model));
        if (model.HealthBonus < 0)
            throw new ArgumentException("Health bonus cannot be negative.", nameof(model));
        if (model.ArmorBonus < 0)
            throw new ArgumentException("Armor bonus cannot be negative.", nameof(model));
        if (model.AttackBonus < 0)
            throw new ArgumentException("Attack bonus cannot be negative.", nameof(model));
        if (model.ResistBonus < 0 || model.ResistBonus > DamageCalculator.MaxResist)
            throw new ArgumentException($"Resist bonus must be between 0 and {DamageCalculator.MaxResist}.", nameof(model));
        if (model.SpeedBonus < -50)
            throw new ArgumentException("Speed bonus cannot be lower than -50.", nameof(model));

        Model = model;
    }

    public Item(string name, int healthBonus = 0, int armorBonus = 0, int attackBonus = 0, int resistBonus = 0, int speedBonus = 0)
        : this(new ItemModel(name, healthBonus, armorBonus, attackBonus, resistBonus, speedBonus))
    {
    }
}
