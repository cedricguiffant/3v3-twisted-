using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Abilities.Effects;
using Twisted3v3.Combat.StatusEffects;

namespace Twisted3v3.Combat
{
    /// <summary>
    /// Piège au sol : s'arme après un court délai, puis déclenche sur le premier
    /// ennemi entrant (dégâts + immobilisation) avant de disparaître. Expire seul
    /// s'il n'est jamais déclenché. (Piège de Lumière de Lirael.)
    /// </summary>
    public sealed class TrapZone : MonoBehaviour
    {
        private Champion _owner;
        private float _radius, _armDelay, _lifetime, _damage, _rootDuration;
        private LayerMask _mask;
        private bool _armed;

        public static TrapZone Spawn(Champion owner, Vector3 position, float radius,
            float armDelay, float lifetime, float damage, float rootDuration,
            LayerMask mask, Color color)
        {
            var go = new GameObject("TrapZone");
            go.transform.position = position;

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.transform.SetParent(go.transform, false);
            marker.transform.localScale = new Vector3(radius * 2f, 0.04f, radius * 2f);
            marker.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            var col = marker.GetComponent<Collider>(); if (col != null) Destroy(col);
            marker.GetComponent<Renderer>().material.color = new Color(color.r, color.g, color.b, 0.45f);

            var t = go.AddComponent<TrapZone>();
            t._owner = owner; t._radius = radius; t._armDelay = armDelay; t._lifetime = lifetime;
            t._damage = damage; t._rootDuration = rootDuration; t._mask = mask;
            return t;
        }

        private void Update()
        {
            if (_owner == null) { Destroy(gameObject); return; }

            if (!_armed)
            {
                _armDelay -= Time.deltaTime;
                if (_armDelay <= 0f) _armed = true;
                return;
            }

            _lifetime -= Time.deltaTime;
            if (_lifetime <= 0f) { Destroy(gameObject); return; }

            var enemies = AbilityUtils.GetEnemiesInRadius(_owner, transform.position, _radius, _mask);
            if (enemies.Count == 0) return;

            var victim = enemies[0];
            victim.TakeDamage(new DamageInfo(_damage, DamageType.Magical, _owner));
            if (AbilityUtils.TryGetChampion(victim, out var champ))
                champ.Status.Apply(new RootEffect(_rootDuration));
            Destroy(gameObject);
        }
    }
}
