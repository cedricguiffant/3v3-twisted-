using System.Collections.Generic;
using UnityEngine;

namespace Twisted3v3.Champions
{
    /// <summary>
    /// Source de vérité de la visibilité d'un champion. Capture au Start l'état de
    /// référence de chaque renderer (ex: capsule DÉSACTIVÉE sous un modèle 3D, modèle
    /// activé) et le restaure à l'identique — au lieu d'un « tout réactiver » aveugle
    /// qui superposait capsule et modèle après un respawn. Utilisé par le
    /// RespawnController (mort/respawn) et le VisionSystem (buissons).
    /// </summary>
    [RequireComponent(typeof(Champion))]
    public sealed class ChampionVisuals : MonoBehaviour
    {
        private readonly Dictionary<Renderer, bool> _baseline = new();
        private bool _captured;

        /// <summary>Récupère (ou ajoute) le composant — pattern explicite, l'opérateur
        /// ?? ne fonctionne pas avec le faux null d'Unity.</summary>
        public static ChampionVisuals Of(Champion champion)
        {
            var visuals = champion.GetComponent<ChampionVisuals>();
            if (visuals == null) visuals = champion.gameObject.AddComponent<ChampionVisuals>();
            return visuals;
        }

        private void Start()
        {
            if (!_captured) CaptureBaseline();
        }

        /// <summary>
        /// Mémorise l'état voulu de chaque renderer. Appelé au Start (après que le
        /// modèle 3D éventuel a été instancié en Awake) ; ré-appelable si le visuel
        /// change en cours de partie.
        /// </summary>
        public void CaptureBaseline()
        {
            _baseline.Clear();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r != null) _baseline[r] = r.enabled;
            _captured = true;
        }

        /// <summary>
        /// Cache tout (mort, dissimulation) ou restaure EXACTEMENT l'état de référence
        /// — un renderer désactivé de base (capsule remplacée) reste désactivé.
        /// Les visuels apparus après la capture (auras suiveuses...) ne sont pas touchés.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (!_captured) CaptureBaseline();
            foreach (var entry in _baseline)
                if (entry.Key != null) entry.Key.enabled = visible && entry.Value;
        }
    }
}
