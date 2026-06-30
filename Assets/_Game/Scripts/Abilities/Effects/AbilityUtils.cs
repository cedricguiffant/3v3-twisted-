using System.Collections.Generic;
using UnityEngine;
using Twisted3v3.Champions;
using Twisted3v3.Combat;

namespace Twisted3v3.Abilities.Effects
{
    /// <summary>
    /// Helpers de ciblage partagés par les capacités : détection de cibles dans une
    /// zone (cercle / cône) avec filtrage par équipe. Évite de dupliquer la logique
    /// de Physics.OverlapSphere dans chaque sort.
    /// </summary>
    public static class AbilityUtils
    {
        // Buffer réutilisé pour éviter les allocations à chaque cast.
        private static readonly Collider[] _buffer = new Collider[32];

        /// <summary>Ennemis dans un rayon autour d'un point.</summary>
        public static List<IDamageable> GetEnemiesInRadius(Champion caster, Vector3 center,
            float radius, LayerMask mask)
        {
            var results = new List<IDamageable>();
            int count = Physics.OverlapSphereNonAlloc(center, radius, _buffer, mask);
            for (int i = 0; i < count; i++)
            {
                if (_buffer[i].TryGetComponent<IDamageable>(out var dmg) && caster.IsEnemy(dmg))
                    results.Add(dmg);
            }
            return results;
        }

        /// <summary>Ennemis dans un cône (origine, direction, angle total en degrés).</summary>
        public static List<IDamageable> GetEnemiesInCone(Champion caster, Vector3 origin,
            Vector3 direction, float range, float angleDegrees, LayerMask mask)
        {
            var results = new List<IDamageable>();
            float halfAngle = angleDegrees * 0.5f;
            direction.y = 0f;
            direction.Normalize();

            int count = Physics.OverlapSphereNonAlloc(origin, range, _buffer, mask);
            for (int i = 0; i < count; i++)
            {
                if (!_buffer[i].TryGetComponent<IDamageable>(out var dmg)) continue;
                if (!caster.IsEnemy(dmg)) continue;

                Vector3 toTarget = dmg.Transform.position - origin;
                toTarget.y = 0f;
                if (Vector3.Angle(direction, toTarget) <= halfAngle)
                    results.Add(dmg);
            }
            return results;
        }

        /// <summary>Applique des dégâts à une liste de cibles.</summary>
        public static void DealDamage(IReadOnlyList<IDamageable> targets, in DamageInfo info)
        {
            for (int i = 0; i < targets.Count; i++)
                targets[i].TakeDamage(info);
        }

        /// <summary>Récupère le composant Champion d'une cible (pour appliquer un StatusEffect).</summary>
        public static bool TryGetChampion(IDamageable target, out Champion champion)
        {
            champion = null;
            return target != null && target.Transform != null
                   && target.Transform.TryGetComponent(out champion);
        }
    }
}
