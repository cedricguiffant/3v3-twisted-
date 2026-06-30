using UnityEngine;

namespace Twisted3v3.Progression
{
    /// <summary>
    /// Courbe d'expérience. Renvoie l'XP nécessaire pour passer d'un niveau au suivant.
    /// Courbe linéaire croissante (rythme rapide adapté aux parties 12–18 min).
    /// </summary>
    public static class ExperienceTable
    {
        public const int MaxLevel = 18;

        private const float BaseXp = 180f;   // niveau 1 → 2
        private const float XpPerLevel = 100f; // +100 par palier

        /// <summary>XP requise pour passer de <paramref name="level"/> à level+1.</summary>
        public static float XpToNext(int level)
        {
            if (level >= MaxLevel) return float.PositiveInfinity;
            return BaseXp + XpPerLevel * (level - 1);
        }

        /// <summary>XP cumulée totale pour atteindre <paramref name="level"/> depuis le niveau 1.</summary>
        public static float TotalXpForLevel(int level)
        {
            float total = 0f;
            for (int l = 1; l < Mathf.Clamp(level, 1, MaxLevel); l++)
                total += XpToNext(l);
            return total;
        }
    }
}
