using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Abilities.Effects;

namespace Twisted3v3.Combat
{
    /// <summary>
    /// Zone de dégâts persistante (DoT) : inflige des dégâts aux ennemis présents
    /// dans un rayon à intervalle régulier, pendant une durée, puis se détruit.
    /// Brique réutilisable (zone brûlante, poison, champ de contrôle...).
    /// </summary>
    public sealed class DamageZone : MonoBehaviour
    {
        private Champion _owner;
        private float _radius, _damagePerTick, _interval, _duration;
        private DamageType _type;
        private LayerMask _mask;

        private float _life, _tickTimer;

        public static DamageZone Spawn(Champion owner, Vector3 position, float radius,
            float damagePerTick, float interval, float duration, DamageType type,
            LayerMask mask, Color color)
        {
            var go = new GameObject("DamageZone");
            go.transform.position = position;

            // Marqueur visuel plat.
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.transform.SetParent(go.transform, false);
            marker.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
            marker.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            var col = marker.GetComponent<Collider>(); if (col != null) Destroy(col);
            var rend = marker.GetComponent<Renderer>();
            if (rend != null) rend.material.color = new Color(color.r, color.g, color.b, 0.4f);
            go.AddComponent<Twisted3v3.VFX.MaterialCleanup>(); // libère la copie de matériau

            var zone = go.AddComponent<DamageZone>();
            zone._owner = owner;
            zone._radius = radius;
            zone._damagePerTick = damagePerTick;
            zone._interval = Mathf.Max(0.1f, interval);
            zone._duration = duration;
            zone._type = type;
            zone._mask = mask;
            zone._life = duration;
            return zone;
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f || _owner == null) { Destroy(gameObject); return; }

            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f) return;
            _tickTimer = _interval;

            var targets = AbilityUtils.GetEnemiesInRadius(_owner, transform.position, _radius, _mask);
            var info = new DamageInfo(_damagePerTick, _type, _owner);
            AbilityUtils.DealDamage(targets, info);
        }
    }
}
