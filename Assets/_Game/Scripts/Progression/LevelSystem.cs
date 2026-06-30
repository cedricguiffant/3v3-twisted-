using System;
using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;

namespace Twisted3v3.Progression
{
    /// <summary>
    /// Gère l'XP, le niveau et les points de compétence d'un champion. C'est
    /// l'autorité de progression : il accumule l'XP et délègue la montée de niveau
    /// (re-calcul des stats) à <see cref="Champion.LevelUp"/>.
    /// </summary>
    [RequireComponent(typeof(Champion))]
    public sealed class LevelSystem : MonoBehaviour
    {
        private Champion _champion;

        public float CurrentXp { get; private set; }
        public float XpToNextLevel => ExperienceTable.XpToNext(_champion.Level);
        public int SkillPoints { get; private set; }

        public event Action<LevelSystem> OnXpChanged;
        public event Action<LevelSystem> OnLevelChanged;

        private void Awake()
        {
            _champion = GetComponent<Champion>();
            // Point de compétence du niveau 1 (disponible dès le spawn).
            SkillPoints = 1;
        }

        /// <summary>Ajoute de l'XP et déclenche autant de montées de niveau que nécessaire.</summary>
        public void GainExperience(float amount)
        {
            if (amount <= 0f || _champion.Level >= Champion.MaxLevel) return;
            CurrentXp += amount;

            while (_champion.Level < Champion.MaxLevel && CurrentXp >= XpToNextLevel)
            {
                CurrentXp -= XpToNextLevel;
                if (_champion.LevelUp())
                {
                    SkillPoints++;
                    OnLevelChanged?.Invoke(this);
                }
            }
            OnXpChanged?.Invoke(this);
        }

        /// <summary>
        /// Dépense un point pour monter une capacité d'un rang. L'ultime (R) est
        /// limité par le niveau du champion : rang 1 à 6, rang 2 à 11, rang 3 à 16.
        /// </summary>
        public bool TryLevelAbility(AbilitySlot slot)
        {
            if (SkillPoints <= 0) return false;

            var inst = _champion.Abilities.GetSlot(slot);
            if (inst == null) return false;

            if (slot == AbilitySlot.R)
            {
                int required = inst.Rank == 0 ? 6 : inst.Rank == 1 ? 11 : 16;
                if (_champion.Level < required) return false;
            }

            if (!_champion.Abilities.LevelUp(slot)) return false;
            SkillPoints--;
            return true;
        }
    }
}
