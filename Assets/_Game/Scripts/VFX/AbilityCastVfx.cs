using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Abilities;
using Twisted3v3.Champions;

namespace Twisted3v3.VFX
{
    /// <summary>
    /// Déclenche une onde de cast colorée (couleur signature du champion) à chaque
    /// lancement de sort, en s'abonnant à <c>AbilitySystem.OnAbilityCast</c>.
    /// Purement cosmétique. La couleur est assignée par l'outil d'art pass éditeur.
    /// </summary>
    [RequireComponent(typeof(Champion))]
    public sealed class AbilityCastVfx : MonoBehaviour
    {
        [SerializeField] private Color _color = Color.white;

        private Champion _champion;

        /// <summary>Couleur signature (utilisée par l'éditeur / la factory).</summary>
        public void SetColor(Color color) => _color = color;

        private void Start()
        {
            _champion = GetComponent<Champion>();
            if (_champion.Abilities != null) _champion.Abilities.OnAbilityCast += HandleCast;
        }

        private void OnDestroy()
        {
            if (_champion != null && _champion.Abilities != null)
                _champion.Abilities.OnAbilityCast -= HandleCast;
        }

        private void HandleCast(AbilitySlot slot, AbilityInstance instance)
        {
            // L'ultime a droit à une onde plus marquée et un son plus lourd.
            if (slot == AbilitySlot.R)
            {
                SpellVfx.CastBurst(transform.position, _color);
                SpellVfx.CastBurst(transform.position, Color.Lerp(_color, Color.white, 0.5f));
                Audio.Sfx.Play(Audio.SfxId.CastHeavy, transform.position);
            }
            else
            {
                SpellVfx.CastBurst(transform.position, _color);
                Audio.Sfx.Play(Audio.SfxId.CastLight, transform.position, 0.8f);
            }
        }
    }
}
