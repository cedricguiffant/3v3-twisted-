using System.Collections.Generic;
using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Stats;

namespace Twisted3v3.Items
{
    /// <summary>Famille d'item — pilote l'organisation du shop et les règles d'unicité.</summary>
    public enum ItemTier
    {
        Starter,
        Boots,
        Legendary,
        Mythic // Un seul Mythique autorisé par build.
    }

    /// <summary>Un bonus de stat fourni par un item.</summary>
    [System.Serializable]
    public struct ItemStat
    {
        public StatType Stat;
        public float Value;
        public ModifierType Modifier;
    }

    /// <summary>
    /// Définition data-driven d'un item. À l'achat, l'inventaire crée des
    /// <see cref="StatModifier"/> (avec l'item comme source) et les pousse dans le
    /// StatSheet ; à la revente, on retire tout par source.
    /// Créer via : Assets > Create > Twisted3v3 > Item.
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Item", fileName = "IT_NewItem")]
    public sealed class ItemData : ScriptableObject
    {
        [Header("Identité")]
        public string DisplayName = "Nouvel Item";
        [TextArea] public string Description;
        public Sprite Icon;
        public ItemTier Tier = ItemTier.Legendary;

        [Header("Économie")]
        public int TotalCost = 1000;
        [Tooltip("Items requis pour la recette (combinaison).")]
        public List<ItemData> BuildsFrom = new();

        [Header("Stats fournies")]
        public List<ItemStat> Stats = new();

        [Header("Effet actif / passif (optionnel)")]
        [Tooltip("Comportement spécial branché à l'équipement (ex: actif Mythique).")]
        public ItemEffect Effect;

        /// <summary>Ciblage de rôle pour le shop intelligent / recommandations.</summary>
        public ChampionRole[] RecommendedFor;
    }
}
