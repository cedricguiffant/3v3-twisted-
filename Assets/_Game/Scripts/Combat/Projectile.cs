using System;
using System.Collections.Generic;
using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;

namespace Twisted3v3.Combat
{
    /// <summary>
    /// Projectile générique : avance en ligne droite (ou suit une cible pour les
    /// auto-attaques à tête chercheuse), inflige des dégâts au premier ennemi touché
    /// (ou traverse si <c>pierce</c>), se détruit à l'impact ou à bout de portée.
    /// Brique réutilisable (auto-attaque à distance, skillshots, salves...).
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        private Champion _owner;
        private float _speed, _maxRange, _radius, _damage;
        private DamageType _type;
        private LayerMask _mask;
        private bool _pierce;
        private IDamageable _homing;
        private Action<IDamageable> _onHit;

        private Vector3 _direction;
        private float _traveled;
        private HashSet<IDamageable> _alreadyHit;

        private static readonly Collider[] _buffer = new Collider[16];

        public static Projectile Spawn(Champion owner, Vector3 origin, Vector3 direction,
            float speed, float maxRange, float radius, float damage, DamageType type,
            LayerMask mask, bool pierce, IDamageable homing, Action<IDamageable> onHit, Color color)
        {
            var go = new GameObject("Projectile");
            go.transform.position = origin;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = Vector3.one * Mathf.Max(0.2f, radius * 2f);
            var col = visual.GetComponent<Collider>(); if (col != null) Destroy(col);
            visual.GetComponent<Renderer>().material.color = color;

            var p = go.AddComponent<Projectile>();
            p._owner = owner; p._speed = speed; p._maxRange = maxRange; p._radius = Mathf.Max(0.25f, radius);
            p._damage = damage; p._type = type; p._mask = mask; p._pierce = pierce;
            p._homing = homing; p._onHit = onHit;
            direction.y = 0f;
            p._direction = direction.sqrMagnitude > 0.001f ? direction.normalized : owner.transform.forward;
            if (pierce) p._alreadyHit = new HashSet<IDamageable>();
            return p;
        }

        private void Update()
        {
            if (_owner == null) { Destroy(gameObject); return; }

            // Tête chercheuse : se réoriente vers la cible tant qu'elle vit.
            if (_homing != null && !_homing.IsDead)
            {
                Vector3 to = _homing.Transform.position - transform.position; to.y = 0f;
                if (to.sqrMagnitude > 0.01f) _direction = to.normalized;
            }

            float step = _speed * Time.deltaTime;
            transform.position += _direction * step;
            _traveled += step;

            int n = Physics.OverlapSphereNonAlloc(transform.position, _radius, _buffer, _mask);
            for (int i = 0; i < n; i++)
            {
                if (!_buffer[i].TryGetComponent<IDamageable>(out var d)) continue;
                if (d.IsDead || !_owner.IsEnemy(d)) continue;
                if (_pierce) { if (_alreadyHit.Contains(d)) continue; _alreadyHit.Add(d); }

                d.TakeDamage(new DamageInfo(_damage, _type, _owner));
                _onHit?.Invoke(d);
                if (!_pierce) { Destroy(gameObject); return; }
            }

            if (_traveled >= _maxRange) Destroy(gameObject);
        }
    }
}
