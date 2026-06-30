using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Abilities.Effects;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Vexor
{
    /// <summary>
    /// Z de Vexor — Bond Spectral. Dash dans la direction visée, infligeant des dégâts
    /// aux ennemis traversés à l'arrivée (engage / poursuite).
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Vexor/Z - Bond Spectral",
                     fileName = "AB_Vexor_Z")]
    public sealed class Vexor_Z_BondSpectral : AbilityData
    {
        public float DashDistance = 6f;
        public float DashSpeed = 28f;
        public float ImpactRadius = 2.5f;
        public LayerMask TargetMask = ~0;
        public float[] BaseDamageByRank = { 55f, 85f, 115f, 145f, 175f };
        public float AdRatio = 0.5f;

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            Vector3 destination = caster.transform.position + ctx.AimDirection * DashDistance;
            caster.Motor.Dash(destination, DashSpeed);

            float damage = GetByRank(BaseDamageByRank, ctx.Rank)
                         + AdRatio * caster.Stats.Value(StatType.AttackDamage);
            var info = new DamageInfo(damage, DamageType.Physical, caster);
            var targets = AbilityUtils.GetEnemiesInRadius(caster, destination, ImpactRadius, TargetMask);
            AbilityUtils.DealDamage(targets, info);
        }
    }
}
