using UnityEngine;
using Twisted3v3.Champions;
using Twisted3v3.Items;

namespace Twisted3v3.Net
{
    /// <summary>
    /// Pont shop ↔ réseau. Les items voyagent par leur index dans l'ItemCatalog
    /// (le même asset des deux côtés). Côté client multijoueur, le ShopUI passe
    /// par <see cref="InterceptBuy"/>/<see cref="InterceptSell"/> : l'achat est
    /// validé par le serveur, qui renvoie l'or et l'inventaire à jour.
    /// </summary>
    public static class NetShop
    {
        private static ItemCatalog _catalog;

        public static ItemCatalog Catalog
        {
            get
            {
                if (_catalog == null) _catalog = Resources.Load<ItemCatalog>("ItemCatalog");
                return _catalog;
            }
            set { if (value != null) _catalog = value; }
        }

        public static int IndexOf(ItemData item)
        {
            var catalog = Catalog;
            return catalog != null ? catalog.Items.IndexOf(item) : -1;
        }

        public static ItemData Resolve(int index)
        {
            var catalog = Catalog;
            return catalog != null && index >= 0 && index < catalog.Items.Count
                ? catalog.Items[index] : null;
        }

        /// <summary>Vrai si ce champion est piloté par le client réseau local.</summary>
        private static bool IsNetworkOwned(Champion champion) =>
            NetRunner.IsClient && NetRunner.Instance.Client.OwnChampion == champion;

        /// <summary>Renvoie true si l'achat a été délégué au serveur (client MP).</summary>
        public static bool InterceptBuy(Champion champion, ItemData item)
        {
            if (!IsNetworkOwned(champion)) return false;
            int index = IndexOf(item);
            if (index >= 0) NetRunner.Instance.Client.SendBuy(index);
            return true;
        }

        /// <summary>Renvoie true si la vente a été déléguée au serveur (client MP).</summary>
        public static bool InterceptSell(Champion champion, int slotIndex)
        {
            if (!IsNetworkOwned(champion)) return false;
            NetRunner.Instance.Client.SendSell(slotIndex);
            return true;
        }
    }
}
