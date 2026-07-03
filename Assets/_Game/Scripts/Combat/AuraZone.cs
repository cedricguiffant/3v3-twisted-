using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Combat.StatusEffects;

namespace Twisted3v3.Combat
{
    /// <summary>
    /// Zone d'aura persistante : à intervalle régulier, blesse (et ralentit) les
    /// ennemis et soigne les alliés présents dans le rayon. Peut suivre son lanceur
    /// (aura) ou rester au sol (zone). Brique réutilisable (Forêt Primordiale,
    /// Forteresse Impériale, Épines Vivantes...).
    /// </summary>
    public sealed class AuraZone : MonoBehaviour
    {
        private Champion _owner;
        private float _radius, _interval, _duration, _enemyDamage, _allyHeal, _slow, _slowDuration;
        private LayerMask _mask;
        private bool _follow;
        private float _life, _tickTimer;

        private static readonly Collider[] _buffer = new Collider[48];

        public static AuraZone Spawn(Champion owner, Vector3 position, float radius,
            float enemyDamagePerTick, float allyHealPerTick, float interval, float duration,
            float slow, float slowDuration, LayerMask mask, Color color, bool follow)
        {
            var go = new GameObject("AuraZone");
            go.transform.position = position;
            if (follow) go.transform.SetParent(owner.transform, true);

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.transform.SetParent(go.transform, false);
            marker.transform.localScale = new Vector3(radius * 2f, 0.04f, radius * 2f);
            marker.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            var col = marker.GetComponent<Collider>(); if (col != null) Destroy(col);
            marker.GetComponent<Renderer>().material.color = new Color(color.r, color.g, color.b, 0.3f);
            go.AddComponent<Twisted3v3.VFX.MaterialCleanup>(); // libère la copie de matériau

            var z = go.AddComponent<AuraZone>();
            z._owner = owner; z._radius = radius; z._interval = Mathf.Max(0.1f, interval);
            z._duration = duration; z._enemyDamage = enemyDamagePerTick; z._allyHeal = allyHealPerTick;
            z._slow = slow; z._slowDuration = slowDuration; z._mask = mask; z._follow = follow;
            z._life = duration;
            return z;
        }

        private void Update()
        {
            if (_owner == null) { Destroy(gameObject); return; }
            if (_duration > 0f)
            {
                _life -= Time.deltaTime;
                if (_life <= 0f) { Destroy(gameObject); return; }
            }

            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f) return;
            _tickTimer = _interval;

            int n = Physics.OverlapSphereNonAlloc(transform.position, _radius, _buffer, _mask);
            for (int i = 0; i < n; i++)
            {
                if (!_buffer[i].TryGetComponent<IDamageable>(out var d) || d.IsDead) continue;

                if (_owner.IsEnemy(d))
                {
                    if (_enemyDamage > 0f)
                        d.TakeDamage(new DamageInfo(_enemyDamage, DamageType.Magical, _owner));
                    if (_slow > 0f && d.Transform.TryGetComponent<Champion>(out var ec))
                        ec.Status.Apply(new SlowEffect(_slow, _slowDuration));
                }
                else if (_owner.IsAlly(d) && _allyHeal > 0f)
                {
                    d.Heal(_allyHeal, _owner);
                }
            }
        }
    }
}
