using System;
using UnityEngine;
using Twisted3v3.Core;

namespace Twisted3v3.Combat
{
    /// <summary>
    /// Bâtiment destructible (tour, Nexus...) : possède des PV, appartient à une
    /// équipe, est ciblable (<see cref="IDamageable"/>) et expose sa vie à l'UI.
    /// Peut être rendu invulnérable (ex: Nexus protégé par ses tours).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class Structure : MonoBehaviour, IDamageable, IHealthInfo
    {
        [SerializeField] private Team _team = Team.Blue;
        [SerializeField] private float _maxHealth = 2000f;

        private float _health;
        private bool _destroyed;

        /// <summary>Quand vrai, ignore tous les dégâts (ex: Nexus tant qu'une tour tient).</summary>
        public bool IsInvulnerable { get; set; }

        public Team Team => _team;
        public Transform Transform => transform;
        public bool IsDead => _destroyed;
        public float CurrentHealth => _health;
        public float MaxHealth => _maxHealth;

        public event Action<Structure> OnDestroyed;

        public void Configure(Team team, float maxHealth)
        {
            _team = team;
            _maxHealth = maxHealth;
            _health = maxHealth;
        }

        private void Awake()
        {
            if (_health <= 0f) _health = _maxHealth;
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (_destroyed || IsInvulnerable) return;
            _health -= info.Amount;
            if (_health <= 0f)
            {
                _health = 0f;
                _destroyed = true;
                OnDestroyed?.Invoke(this);
                // Effondrement visuel : on masque le bâtiment.
                foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
                foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
                // Les ruines ne bloquent plus le passage (obstacle NavMesh éteint).
                if (TryGetComponent<UnityEngine.AI.NavMeshObstacle>(out var obstacle))
                    obstacle.enabled = false;
            }
        }

        public void Heal(float amount, object source = null)
        {
            if (_destroyed) return;
            _health = Mathf.Min(_maxHealth, _health + amount);
        }
    }
}
