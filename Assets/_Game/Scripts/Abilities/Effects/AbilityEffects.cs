using System;
using System.Collections.Generic;
using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Combat;
using Twisted3v3.Combat.StatusEffects;

namespace Twisted3v3.Abilities.Effects
{
    /// <summary>
    /// Façade d'effets *composables* pour les capacités : dégâts (cible / zone / cône),
    /// contrôles de foule, dash et zones de dégâts persistantes. Chaque sort compose
    /// ces briques au lieu de ré-implémenter Physics/Projectile/StatusEffect à la main,
    /// ce qui supprime la duplication entre les ~30 capacités.
    ///
    /// Toutes les méthodes sont sans allocation superflue et sûres si la cible est nulle.
    /// </summary>
    public static class AbilityEffects
    {
        // -------------------------------------------------------------------- Dégâts
        /// <summary>Dégâts à une cible unique (avec vol de vie optionnel côté lanceur).</summary>
        public static void Damage(Champion caster, IDamageable victim, float amount,
            DamageType type, bool crit = false, bool canLifesteal = false)
        {
            if (victim == null || victim.IsDead) return;
            victim.TakeDamage(new DamageInfo(amount, type, caster, crit, canLifesteal));
        }

        /// <summary>
        /// Dégâts à tous les ennemis dans un rayon. Renvoie le nombre touché ; appelle
        /// <paramref name="onEnemyChampion"/> pour chaque champion touché (marques, CC...).
        /// </summary>
        public static int DamageArea(Champion caster, Vector3 center, float radius,
            float amount, DamageType type, LayerMask mask, Action<Champion> onEnemyChampion = null)
        {
            var targets = AbilityUtils.GetEnemiesInRadius(caster, center, radius, mask);
            return ApplyToTargets(caster, targets, amount, type, onEnemyChampion);
        }

        /// <summary>Dégâts à tous les ennemis dans un cône (origine, direction, angle total).</summary>
        public static int DamageCone(Champion caster, Vector3 origin, Vector3 direction,
            float range, float angleDegrees, float amount, DamageType type, LayerMask mask,
            Action<Champion> onEnemyChampion = null)
        {
            var targets = AbilityUtils.GetEnemiesInCone(caster, origin, direction, range, angleDegrees, mask);
            return ApplyToTargets(caster, targets, amount, type, onEnemyChampion);
        }

        private static int ApplyToTargets(Champion caster, List<IDamageable> targets,
            float amount, DamageType type, Action<Champion> onEnemyChampion)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].TakeDamage(new DamageInfo(amount, type, caster));
                if (onEnemyChampion != null && AbilityUtils.TryGetChampion(targets[i], out var champ))
                    onEnemyChampion(champ);
            }
            return targets.Count;
        }

        // ----------------------------------------------------------- Contrôles de foule
        /// <summary>Applique un ralentissement à une cible (si c'est un champion).</summary>
        public static void Slow(IDamageable victim, float amount, float duration)
        {
            if (AbilityUtils.TryGetChampion(victim, out var champ))
                champ.Status.Apply(new SlowEffect(amount, duration));
        }

        /// <summary>Applique un étourdissement à une cible (si c'est un champion).</summary>
        public static void Stun(IDamageable victim, float duration)
        {
            if (AbilityUtils.TryGetChampion(victim, out var champ))
                champ.Status.Apply(new StunEffect(duration));
        }

        /// <summary>Applique une immobilisation à une cible (si c'est un champion).</summary>
        public static void Root(IDamageable victim, float duration)
        {
            if (AbilityUtils.TryGetChampion(victim, out var champ))
                champ.Status.Apply(new RootEffect(duration));
        }

        // -------------------------------------------------------------------- Mobilité
        /// <summary>Dash du lanceur dans une direction, borné par le NavMesh (murs).</summary>
        public static void Dash(Champion caster, Vector3 direction, float distance, float speed)
        {
            if (caster == null) return;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) direction = caster.transform.forward;
            caster.Motor.Dash(caster.transform.position + direction.normalized * distance, speed);
        }

        // ------------------------------------------------------------------ Zone (DoT)
        /// <summary>Zone de dégâts persistante au sol (brasier, nuage...).</summary>
        public static DamageZone Zone(Champion caster, Vector3 position, float radius,
            float damagePerSecond, float duration, DamageType type, LayerMask mask, Color color)
        {
            // La zone tick à ~4 Hz : on convertit le DPS voulu en dégâts par tick.
            const float interval = 0.25f;
            return DamageZone.Spawn(caster, position, radius, damagePerSecond * interval,
                interval, duration, type, mask, color);
        }
    }
}
