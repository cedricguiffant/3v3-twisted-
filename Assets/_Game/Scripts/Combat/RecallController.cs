using UnityEngine;
using Twisted3v3.Champions;

namespace Twisted3v3.Combat
{
    /// <summary>
    /// Repli vers la fontaine (touche B, style MOBA) : canal de quelques secondes,
    /// immobile et sans attaquer, annulé si le champion prend des dégâts. À la fin
    /// du canal, téléporte le champion à la fontaine de son équipe.
    /// </summary>
    [RequireComponent(typeof(Champion))]
    public sealed class RecallController : MonoBehaviour
    {
        [SerializeField] private float _channelTime = 5f;

        private Champion _champion;
        private AutoAttack _autoAttack;
        private float _timer;
        private float _healthAtStart;

        public bool IsChanneling { get; private set; }
        public float Progress => IsChanneling ? 1f - Mathf.Clamp01(_timer / _channelTime) : 0f;

        private void Awake()
        {
            _champion = GetComponent<Champion>();
            _autoAttack = GetComponent<AutoAttack>();
        }

        /// <summary>Démarre le canal, ou l'annule s'il est déjà en cours.</summary>
        public void Toggle()
        {
            if (IsChanneling) Cancel();
            else StartChannel();
        }

        private void StartChannel()
        {
            if (_champion.IsDead) return;
            IsChanneling = true;
            _timer = _channelTime;
            _healthAtStart = _champion.Health != null ? _champion.Health.CurrentHealth : 0f;
            _autoAttack?.ClearTarget();
            _champion.Motor.Stop();
            Twisted3v3.UI.FloatingText.Spawn(_champion.transform.position, "Repli…",
                new Color(0.6f, 0.8f, 1f));
        }

        /// <summary>Annule le canal en cours (dégâts subis, ordre du joueur).</summary>
        public void Cancel()
        {
            if (!IsChanneling) return;
            IsChanneling = false;
            Twisted3v3.UI.FloatingText.Spawn(_champion.transform.position, "Repli annulé",
                new Color(0.9f, 0.5f, 0.4f));
        }

        private void Update()
        {
            if (!IsChanneling) return;
            if (_champion.IsDead) { IsChanneling = false; return; }

            // Perte de PV pendant le canal (dégâts) → annulé. Détecté par comparaison
            // plutôt que par l'événement OnDamaged : fonctionne aussi côté client
            // multijoueur, où les PV du champion réseau viennent des snapshots
            // (TakeDamage n'y est jamais appelé localement).
            if (_champion.Health != null && _champion.Health.CurrentHealth < _healthAtStart - 0.01f)
            {
                Cancel();
                return;
            }

            _champion.Motor.Stop(); // reste immobile pendant tout le canal
            _timer -= Time.deltaTime;
            if (_timer <= 0f) Teleport();
        }

        private void Teleport()
        {
            IsChanneling = false;
            var fountain = FindFountain(_champion.Team);
            if (fountain == null) return;
            _champion.Motor.Warp(fountain.transform.position);
        }

        private static FountainZone FindFountain(Core.Team team)
        {
            foreach (var f in Object.FindObjectsByType<FountainZone>(FindObjectsSortMode.None))
                if (f.Team == team) return f;
            return null;
        }
    }
}
