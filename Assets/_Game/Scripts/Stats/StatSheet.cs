using System.Collections.Generic;

namespace Twisted3v3.Stats
{
    /// <summary>
    /// Conteneur runtime de toutes les stats d'une unité.
    /// Initialisé depuis les valeurs de base d'un <c>ChampionData</c>,
    /// modifié ensuite par items, buffs et capacités.
    /// </summary>
    public sealed class StatSheet
    {
        private readonly Dictionary<StatType, Stat> _stats = new();

        public Stat Get(StatType type)
        {
            if (!_stats.TryGetValue(type, out var stat))
            {
                stat = new Stat(0f);
                _stats[type] = stat;
            }
            return stat;
        }

        /// <summary>Raccourci pour lire la valeur finale d'une stat.</summary>
        public float Value(StatType type) => Get(type).Value;

        public void SetBase(StatType type, float baseValue) => Get(type).BaseValue = baseValue;

        public void AddModifier(StatType type, StatModifier modifier) => Get(type).AddModifier(modifier);

        /// <summary>Retire tous les modificateurs d'une source sur l'ensemble des stats.</summary>
        public void RemoveAllFromSource(object source)
        {
            foreach (var stat in _stats.Values)
                stat.RemoveAllFromSource(source);
        }
    }
}
