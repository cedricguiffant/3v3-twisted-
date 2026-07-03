using System;
using System.Collections.Generic;
using UnityEngine;
using Twisted3v3.Champions;
using Twisted3v3.Combat;
using Twisted3v3.Economy;
using Twisted3v3.Stats;

namespace Twisted3v3.Items
{
    /// <summary>
    /// Cœur *pur* (sans MonoBehaviour) de l'application des stats d'un item sur un
    /// <see cref="StatSheet"/>. Isolé ici pour être testable en EditMode sans scène.
    /// Un item pousse un <see cref="StatModifier"/> par stat, avec l'item comme
    /// source, ce qui permet un retrait propre à la revente via RemoveAllFromSource.
    /// </summary>
    public static class ItemModifiers
    {
        public static void Apply(StatSheet sheet, ItemData item)
        {
            if (sheet == null || item == null) return;
            foreach (var s in item.Stats)
                sheet.AddModifier(s.Stat, new StatModifier(s.Value, s.Modifier, item));
        }

        public static void Remove(StatSheet sheet, ItemData item)
        {
            if (sheet == null || item == null) return;
            sheet.RemoveAllFromSource(item);
        }
    }

    /// <summary>
    /// Inventaire runtime d'un champion : jusqu'à 6 items. À l'achat, débite l'or,
    /// applique les stats (via <see cref="ItemModifiers"/>) et branche l'éventuel
    /// <see cref="ItemEffect"/> ; à la revente, retire les stats par source et
    /// rembourse une fraction. Gère l'unicité (un seul Mythique, une seule paire de
    /// bottes). Aucune logique d'input/UI ici — le <c>ShopUI</c> l'appelle.
    /// </summary>
    [RequireComponent(typeof(Champion))]
    public sealed class Inventory : MonoBehaviour
    {
        public const int MaxSlots = 6;

        [Tooltip("Fraction du prix remboursée à la revente (0.7 = 70%).")]
        [SerializeField, Range(0f, 1f)] private float _sellRefund = 0.7f;

        private Champion _champion;
        private GoldWallet _wallet;
        private readonly List<ItemData> _items = new(MaxSlots);

        /// <summary>Déclenché quand le contenu de l'inventaire change (achat/revente).</summary>
        public event Action OnChanged;

        public IReadOnlyList<ItemData> Items => _items;
        public bool IsFull => _items.Count >= MaxSlots;

        private void Awake()
        {
            _champion = GetComponent<Champion>();
            _wallet = GetComponent<GoldWallet>();
        }

        /// <summary>Vrai si l'inventaire contient déjà un item de ce palier (unicité).</summary>
        public bool HasTier(ItemTier tier)
        {
            for (int i = 0; i < _items.Count; i++)
                if (_items[i].Tier == tier) return true;
            return false;
        }

        /// <summary>Raison d'un refus d'achat (pour le feedback UI). None = achat possible.</summary>
        public enum PurchaseError { None, NoItem, Full, NotEnoughGold, DuplicateUnique }

        public PurchaseError CanBuy(ItemData item)
        {
            if (item == null) return PurchaseError.NoItem;
            if (IsFull) return PurchaseError.Full;
            if ((item.Tier == ItemTier.Mythic || item.Tier == ItemTier.Boots) && HasTier(item.Tier))
                return PurchaseError.DuplicateUnique;
            if (_wallet != null && _wallet.Gold < item.TotalCost) return PurchaseError.NotEnoughGold;
            return PurchaseError.None;
        }

        /// <summary>Tente d'acheter un item : débite l'or puis l'équipe. Renvoie false si refusé.</summary>
        public bool TryBuy(ItemData item)
        {
            if (CanBuy(item) != PurchaseError.None) return false;
            if (_wallet != null && !_wallet.TrySpend(item.TotalCost)) return false;

            _items.Add(item);
            Equip(item);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Revend l'item du slot donné et rembourse une fraction du prix.</summary>
        public bool SellAt(int index)
        {
            if (index < 0 || index >= _items.Count) return false;
            var item = _items[index];
            _items.RemoveAt(index);
            Unequip(item);
            _wallet?.AddGold(Mathf.RoundToInt(item.TotalCost * _sellRefund));
            OnChanged?.Invoke();
            return true;
        }

        // --- Équipement : applique/retire les stats en préservant les PV/mana courants ---
        private void Equip(ItemData item)
        {
            if (_champion.Stats == null) return;
            float prevMaxHp = _champion.Stats.Value(StatType.MaxHealth);
            float prevMaxMana = _champion.Stats.Value(StatType.MaxMana);

            ItemModifiers.Apply(_champion.Stats, item);

            // Répercute les gains de PV/Mana max sur les valeurs courantes (style MOBA).
            _champion.Health?.OnMaxHealthIncreased(prevMaxHp);
            _champion.RestoreMana(Mathf.Max(0f, _champion.Stats.Value(StatType.MaxMana) - prevMaxMana));

            if (item.Effect != null) item.Effect.OnEquip(_champion);
        }

        private void Unequip(ItemData item)
        {
            if (_champion.Stats == null) return;
            ItemModifiers.Remove(_champion.Stats, item);
            // Revente d'un item de PV : les PV courants ne doivent pas dépasser le
            // nouveau maximum. (Le mana, lui, se clampe à la frame suivante via RegenMana.)
            _champion.Health?.ClampToMax();
            if (item.Effect != null) item.Effect.OnUnequip(_champion);
        }
    }
}
