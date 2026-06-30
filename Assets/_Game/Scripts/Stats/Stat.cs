using System;
using System.Collections.Generic;

namespace Twisted3v3.Stats
{
    /// <summary>
    /// Une statistique unique : valeur de base + liste de modificateurs.
    /// La valeur finale est mise en cache et recalculée seulement quand un
    /// modificateur change (pattern "dirty flag").
    /// </summary>
    public sealed class Stat
    {
        public float BaseValue
        {
            get => _baseValue;
            set { _baseValue = value; _isDirty = true; }
        }

        /// <summary>Déclenché quand la valeur finale change (UI, recalcul de PV...).</summary>
        public event Action<Stat> OnValueChanged;

        private float _baseValue;
        private float _cachedValue;
        private bool _isDirty = true;
        private readonly List<StatModifier> _modifiers = new();

        public Stat(float baseValue) => _baseValue = baseValue;

        /// <summary>Valeur finale, modificateurs appliqués dans l'ordre Flat → %Add → %Mult.</summary>
        public float Value
        {
            get
            {
                if (_isDirty)
                {
                    _cachedValue = CalculateFinalValue();
                    _isDirty = false;
                }
                return _cachedValue;
            }
        }

        public void AddModifier(StatModifier modifier)
        {
            _modifiers.Add(modifier);
            MarkDirty();
        }

        public bool RemoveModifier(StatModifier modifier)
        {
            if (_modifiers.Remove(modifier)) { MarkDirty(); return true; }
            return false;
        }

        /// <summary>Retire tous les modificateurs venant d'une source (item retiré, buff expiré).</summary>
        public bool RemoveAllFromSource(object source)
        {
            int removed = _modifiers.RemoveAll(m => ReferenceEquals(m.Source, source));
            if (removed > 0) { MarkDirty(); return true; }
            return false;
        }

        private void MarkDirty()
        {
            _isDirty = true;
            OnValueChanged?.Invoke(this);
        }

        private float CalculateFinalValue()
        {
            float flat = _baseValue;
            float percentAdd = 0f;
            float result;

            // 1) Somme des plats
            foreach (var m in _modifiers)
                if (m.Type == ModifierType.Flat) flat += m.Value;

            // 2) Somme des pourcentages additifs, appliquée d'un coup
            foreach (var m in _modifiers)
                if (m.Type == ModifierType.PercentAdditive) percentAdd += m.Value;

            result = flat * (1f + percentAdd);

            // 3) Pourcentages multiplicatifs, appliqués séquentiellement
            foreach (var m in _modifiers)
                if (m.Type == ModifierType.PercentMultiplicative) result *= 1f + m.Value;

            return (float)Math.Round(result, 4);
        }
    }
}
