namespace Twisted3v3.Stats
{
    /// <summary>Ordre d'application des modificateurs sur une stat.</summary>
    public enum ModifierType
    {
        Flat = 100,            // +50 PV
        PercentAdditive = 200, // +10% (cumulés ensemble puis appliqués)
        PercentMultiplicative = 300 // x1.10 (cumulés en multiplication)
    }

    /// <summary>
    /// Un modificateur de stat venant d'une source (item, buff, capacité).
    /// La <see cref="Source"/> permet de retirer proprement tous les modificateurs
    /// d'un item/buff quand il disparaît.
    /// </summary>
    public sealed class StatModifier
    {
        public readonly float Value;
        public readonly ModifierType Type;
        public readonly object Source; // ItemData, BuffInstance, AbilityData...

        public StatModifier(float value, ModifierType type, object source = null)
        {
            Value = value;
            Type = type;
            Source = source;
        }
    }
}
