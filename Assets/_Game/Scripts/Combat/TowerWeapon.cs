using UnityEngine;
using Twisted3v3.Core;

namespace Twisted3v3.Combat
{
    /// <summary>
    /// Arme de tour : à intervalle régulier, frappe l'ennemi (champion/monstre) le
    /// plus proche dans son rayon. Ne vise pas les autres bâtiments. S'appuie sur la
    /// <see cref="Structure"/> du même GameObject (équipe + état détruit).
    /// </summary>
    [RequireComponent(typeof(Structure))]
    public sealed class TowerWeapon : MonoBehaviour
    {
        [SerializeField] private float _range = 11f;
        [SerializeField] private float _attackInterval = 1.1f;
        [SerializeField] private float _damage = 130f;
        [SerializeField] private LayerMask _targetMask = ~0;

        private static readonly Collider[] _buffer = new Collider[32];
        private Structure _structure;
        private float _cooldown;

        private void Awake() => _structure = GetComponent<Structure>();

        private void Update()
        {
            if (_structure.IsDead) return;
            if (_cooldown > 0f) { _cooldown -= Time.deltaTime; return; }

            var target = FindTarget();
            if (target == null) return;

            _cooldown = _attackInterval;
            target.TakeDamage(new DamageInfo(_damage, DamageType.Magical, this));
        }

        private IDamageable FindTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _range, _buffer, _targetMask);
            IDamageable best = null;
            float bestSq = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (!_buffer[i].TryGetComponent<IDamageable>(out var d)) continue;
                if (d.IsDead || d is Structure) continue;                 // pas les bâtiments
                if (d.Team == _structure.Team || d.Team == Team.None) continue; // ennemis seulement
                float sq = (d.Transform.position - transform.position).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = d; }
            }
            return best;
        }
    }
}
