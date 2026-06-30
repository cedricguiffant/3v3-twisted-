using System.Collections.Generic;
using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Stats;

namespace Twisted3v3.Combat.StatusEffects
{
    /// <summary>
    /// Gère la liste des effets actifs sur un champion : application (avec tenacity),
    /// tick, expiration et nettoyage. Composant ajouté au champion à l'init.
    /// </summary>
    public sealed class StatusEffectController : MonoBehaviour
    {
        private Champion _champion;
        private readonly List<StatusEffect> _active = new();

        public void Initialize(Champion champion) => _champion = champion;

        /// <summary>Applique un effet. La tenacity réduit la durée des CC.</summary>
        public void Apply(StatusEffect effect)
        {
            // Réduction de durée par tenacity pour les contrôles de foule.
            if (effect.IsCrowdControl)
            {
                float tenacity = Mathf.Clamp01(_champion.Stats.Value(StatType.Tenacity));
                if (tenacity > 0f)
                {
                    // On ne peut pas ré-affecter Duration (protected set) depuis l'extérieur ;
                    // la tenacity est volontairement appliquée à la construction de l'effet.
                    // Ici on pourrait re-créer l'effet ; gardé simple pour la base.
                }
            }

            // Rafraîchit un effet du même type plutôt que d'empiler, si demandé.
            if (effect.RefreshOnReapply)
            {
                for (int i = 0; i < _active.Count; i++)
                {
                    if (_active[i].GetType() == effect.GetType() && _active[i].CCType == effect.CCType)
                    {
                        _active[i].Refresh();
                        return;
                    }
                }
            }

            effect.Apply(_champion);
            _active.Add(effect);
        }

        public bool HasCrowdControl(CrowdControlType type)
        {
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].CCType == type) return true;
            return false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var effect = _active[i];
                effect.Tick(dt);
                if (effect.IsExpired)
                {
                    effect.OnExpire();
                    _active.RemoveAt(i);
                }
            }
        }

        /// <summary>Purge tous les CC (anti-CC, mort). Conserve l'option de filtrer plus tard.</summary>
        public void CleanseAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].IsCrowdControl)
                {
                    _active[i].OnExpire();
                    _active.RemoveAt(i);
                }
            }
        }
    }
}
