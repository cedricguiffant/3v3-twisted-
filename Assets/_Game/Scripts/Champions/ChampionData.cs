using System.Collections.Generic;
using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Abilities;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions
{
    /// <summary>Une stat de base + sa croissance par niveau.</summary>
    [System.Serializable]
    public struct BaseStatEntry
    {
        public StatType Stat;
        public float BaseValue;
        [Tooltip("Ajouté par niveau au-delà du niveau 1.")]
        public float PerLevel;
    }

    /// <summary>
    /// Définition complète et data-driven d'un champion. Asset partagé : aucune
    /// donnée runtime ici. Le <see cref="Champion"/> en lit les valeurs à l'init.
    /// Créer via : Assets > Create > Twisted3v3 > Champion.
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Champion", fileName = "CH_NewChampion")]
    public sealed class ChampionData : ScriptableObject
    {
        [Header("Identité")]
        public string DisplayName = "Nouveau Champion";
        [TextArea] public string Lore;
        public ChampionRole Role = ChampionRole.Fighter;
        public Sprite Portrait;

        [Header("Modèle")]
        [Tooltip("Prefab visuel (mesh + animator) instancié sous le champion.")]
        public GameObject ModelPrefab;

        [Header("Stats de base (niveau 1) + croissance / niveau")]
        public List<BaseStatEntry> BaseStats = new();

        [Header("Capacités — Passif + Q/Z/E/R")]
        public AbilityData Passive;
        public AbilityData Q;
        public AbilityData Z;
        public AbilityData E;
        public AbilityData R;

        /// <summary>Renvoie les 5 capacités dans l'ordre, en ignorant les vides.</summary>
        public IReadOnlyList<AbilityData> GetAbilities()
        {
            var list = new List<AbilityData>(5);
            if (Passive) list.Add(Passive);
            if (Q) list.Add(Q);
            if (Z) list.Add(Z);
            if (E) list.Add(E);
            if (R) list.Add(R);
            return list;
        }

        /// <summary>Applique les stats de base (avec croissance) à un StatSheet runtime.</summary>
        public void ApplyBaseStats(StatSheet sheet, int level)
        {
            foreach (var entry in BaseStats)
            {
                float value = entry.BaseValue + entry.PerLevel * (level - 1);
                sheet.SetBase(entry.Stat, value);
            }
        }
    }
}
