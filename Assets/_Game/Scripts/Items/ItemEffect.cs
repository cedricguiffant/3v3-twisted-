using UnityEngine;
using Twisted3v3.Champions;

namespace Twisted3v3.Items
{
    /// <summary>
    /// Comportement optionnel attaché à un item (actif Mythique, passif unique...).
    /// Hérite et implémente les hooks utiles. SO partagé : pas d'état runtime ici.
    /// </summary>
    public abstract class ItemEffect : ScriptableObject
    {
        /// <summary>Appelé quand l'item est équipé sur un champion.</summary>
        public virtual void OnEquip(Champion owner) { }

        /// <summary>Appelé quand l'item est retiré/vendu.</summary>
        public virtual void OnUnequip(Champion owner) { }

        /// <summary>Actif déclenché par le joueur (touche d'item). Renvoie false si en cooldown.</summary>
        public virtual bool OnActivate(Champion owner) => false;
    }
}
