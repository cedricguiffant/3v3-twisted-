using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Abilities.Effects;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Ragnor
{
    /// <summary>
    /// R (Ultime) de Ragnor — Apocalypse des Cendres. Ragnor bondit vers un point,
    /// inflige des dégâts d'impact, puis laisse une zone brûlante (DoT) au sol.
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Ragnor/R - Apocalypse des Cendres",
                     fileName = "AB_Ragnor_R")]
    public sealed class Ragnor_R_ApocalypseDesCendres : AbilityData
    {
        [Header("Saut")]
        public float LeapSpeed = 26f;
        public float ImpactRadius = 4f;
        public LayerMask TargetMask = ~0;

        [Header("Dégâts d'impact par rang (ultime : 3 rangs)")]
        public float[] ImpactDamageByRank = { 180f, 280f, 380f };
        public float AdRatio = 0.9f;

        [Header("Zone brûlante")]
        public float ZoneDuration = 4f;
        public float ZoneTickInterval = 0.5f;
        public float[] ZoneDamagePerTickByRank = { 25f, 40f, 55f };

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            Vector3 landing = ctx.GroundPoint;

            // Bond vers le point ciblé.
            caster.Motor.Dash(landing, LeapSpeed);

            // Dégâts d'impact.
            float impact = GetByRank(ImpactDamageByRank, ctx.Rank)
                         + AdRatio * caster.Stats.Value(StatType.AttackDamage);
            var info = new DamageInfo(impact, DamageType.Physical, caster);
            var targets = AbilityUtils.GetEnemiesInRadius(caster, landing, ImpactRadius, TargetMask);
            AbilityUtils.DealDamage(targets, info);

            // Zone brûlante persistante.
            float tick = GetByRank(ZoneDamagePerTickByRank, ctx.Rank);
            DamageZone.Spawn(caster, landing, ImpactRadius, tick, ZoneTickInterval,
                             ZoneDuration, DamageType.Physical, TargetMask,
                             new Color(0.9f, 0.4f, 0.1f));
        }
    }
}
