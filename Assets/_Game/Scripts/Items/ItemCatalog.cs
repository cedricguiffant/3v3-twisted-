using System.Collections.Generic;
using UnityEngine;

namespace Twisted3v3.Items
{
    /// <summary>
    /// Liste ordonnée des items achetables, consommée par le shop. Asset unique
    /// référencé par le <c>ShopUI</c> (ou chargé depuis Resources en secours).
    /// Créer via : Assets > Create > Twisted3v3 > Item Catalog.
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Item Catalog", fileName = "ItemCatalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        public List<ItemData> Items = new();
    }
}
