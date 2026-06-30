using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Lirael
{
    /// <summary>
    /// R (Ultime) de Lirael — Salve Céleste. Décoche une rafale de flèches en éventail
    /// dans la direction visée ; chaque flèche traverse les ennemis (dégâts physiques).
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Lirael/R - Salve Céleste",
                     fileName = "AB_Lirael_R")]
    public sealed class Lirael_R_SalveCeleste : AbilityData
    {
        [Header("Rafale")]
        public int ArrowCount = 7;
        public float SpreadAngle = 50f;
        public float ProjectileSpeed = 26f;
        public float ProjectileRadius = 0.45f;
        public LayerMask TargetMask = ~0;

        [Header("Dégâts par flèche, par rang (ultime : 3 rangs)")]
        public float[] DamagePerArrowByRank = { 60f, 95f, 130f };
        public float AdRatio = 0.4f;

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            float damage = GetByRank(DamagePerArrowByRank, ctx.Rank)
                         + AdRatio * caster.Stats.Value(StatType.AttackDamage);
            Vector3 origin = caster.transform.position + Vector3.up + ctx.AimDirection * 0.6f;

            int count = Mathf.Max(1, ArrowCount);
            float start = -SpreadAngle * 0.5f;
            float stepAngle = count > 1 ? SpreadAngle / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                Quaternion rot = Quaternion.Euler(0f, start + stepAngle * i, 0f);
                Vector3 dir = rot * ctx.AimDirection;
                Projectile.Spawn(caster, origin, dir, ProjectileSpeed, Range,
                    ProjectileRadius, damage, DamageType.Physical, TargetMask,
                    pierce: true, homing: null, onHit: null, color: new Color(0.7f, 0.85f, 1f));
            }
        }
    }
}
