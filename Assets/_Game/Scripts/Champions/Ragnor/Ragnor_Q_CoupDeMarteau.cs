using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Abilities.Effects;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Ragnor
{
    /// <summary>
    /// Q de Ragnor — Coup de Marteau. Frappe en cône devant lui : dégâts physiques
    /// (ratio AD) et repoussée des ennemis touchés.
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Ragnor/Q - Coup de Marteau",
                     fileName = "AB_Ragnor_Q")]
    public sealed class Ragnor_Q_CoupDeMarteau : AbilityData
    {
        [Header("Cône")]
        public float ConeAngle = 75f;
        public LayerMask TargetMask = ~0;

        [Header("Dégâts par rang")]
        public float[] BaseDamageByRank = { 60f, 95f, 130f, 165f, 200f };
        public float AdRatio = 0.8f;

        [Header("Repoussée")]
        public float KnockbackDistance = 3.5f;
        public float KnockbackSpeed = 16f;

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            float damage = GetByRank(BaseDamageByRank, ctx.Rank)
                         + AdRatio * caster.Stats.Value(StatType.AttackDamage);
            var info = new DamageInfo(damage, DamageType.Physical, caster);

            var targets = AbilityUtils.GetEnemiesInCone(
                caster, caster.transform.position, ctx.AimDirection, Range, ConeAngle, TargetMask);

            foreach (var t in targets)
            {
                t.TakeDamage(info);
                if (AbilityUtils.TryGetChampion(t, out var champ))
                    champ.Motor.Knockback(caster.transform.position, KnockbackDistance, KnockbackSpeed);
            }
        }
    }
}
