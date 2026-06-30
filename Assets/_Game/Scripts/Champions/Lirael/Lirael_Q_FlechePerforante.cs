using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Lirael
{
    /// <summary>
    /// Q de Lirael — Flèche Perforante. Skillshot : une flèche qui traverse tous les
    /// ennemis sur sa trajectoire (dégâts physiques, ratio AD).
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Lirael/Q - Flèche Perforante",
                     fileName = "AB_Lirael_Q")]
    public sealed class Lirael_Q_FlechePerforante : AbilityData
    {
        [Header("Projectile")]
        public float ProjectileSpeed = 24f;
        public float ProjectileRadius = 0.5f;
        public LayerMask TargetMask = ~0;

        [Header("Dégâts par rang")]
        public float[] BaseDamageByRank = { 70f, 110f, 150f, 190f, 230f };
        public float AdRatio = 1.0f;

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            float damage = GetByRank(BaseDamageByRank, ctx.Rank)
                         + AdRatio * caster.Stats.Value(StatType.AttackDamage);
            Vector3 origin = caster.transform.position + Vector3.up + ctx.AimDirection * 0.6f;

            Projectile.Spawn(caster, origin, ctx.AimDirection, ProjectileSpeed, Range,
                ProjectileRadius, damage, DamageType.Physical, TargetMask,
                pierce: true, homing: null, onHit: null, color: new Color(1f, 0.95f, 0.6f));
        }
    }
}
