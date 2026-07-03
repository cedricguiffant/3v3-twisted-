using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;

namespace Twisted3v3.Combat
{
    /// <summary>
    /// Fontaine de base : régénère rapidement les PV et le mana des champions alliés
    /// présents dans son rayon (autour du Nexus de l'équipe). Effet MOBA classique.
    /// </summary>
    public sealed class FountainZone : MonoBehaviour
    {
        [SerializeField] private Team _team = Team.Blue;
        [SerializeField] private float _radius = 9f;
        [Tooltip("Fraction des PV max régénérée par seconde.")]
        [SerializeField] private float _healthPerSecond = 0.18f;
        [Tooltip("Fraction du mana max régénérée par seconde.")]
        [SerializeField] private float _manaPerSecond = 0.18f;
        [SerializeField] private int _unitLayer = 10;

        private static readonly Collider[] _buffer = new Collider[32];

        /// <summary>Équipe propriétaire de la fontaine (base alliée).</summary>
        public Team Team => _team;

        /// <summary>Vrai si le point monde est dans le rayon de la fontaine (= « en base »).</summary>
        public bool IsInside(Vector3 worldPos)
        {
            Vector3 d = worldPos - transform.position; d.y = 0f;
            return d.sqrMagnitude <= _radius * _radius;
        }

        public void Configure(Team team) => _team = team;

        private void Update()
        {
            int n = Physics.OverlapSphereNonAlloc(transform.position, _radius, _buffer, 1 << _unitLayer);
            float dt = Time.deltaTime;
            for (int i = 0; i < n; i++)
            {
                if (!_buffer[i].TryGetComponent<Champion>(out var c)) continue;
                if (c.IsDead || c.Team != _team || c.Health == null) continue;

                c.Health.Heal(c.Health.MaxHealth * _healthPerSecond * dt, this, silent: true);
                c.RestoreMana(c.Stats.Value(Stats.StatType.MaxMana) * _manaPerSecond * dt);
            }
        }
    }
}
