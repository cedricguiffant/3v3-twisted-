using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Abilities.Effects;
using Twisted3v3.Combat.StatusEffects;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Ragnor
{
    /// <summary>
    /// Z de Ragnor — Tremblement Volcanique. Onde de choc centrée sur Ragnor :
    /// dégâts physiques en zone et ralentissement.
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Ragnor/Z - Tremblement Volcanique",
                     fileName = "AB_Ragnor_Z")]
    public sealed class Ragnor_Z_TremblementVolcanique : AbilityData
    {
        [Header("Zone")]
        public float Radius = 5f;
        public LayerMask TargetMask = ~0;

        [Header("Dégâts par rang")]
        public float[] BaseDamageByRank = { 70f, 110f, 150f, 190f, 230f };
        public float AdRatio = 0.6f;

        [Header("Ralentissement")]
        [Range(0f, 1f)] public float SlowAmount = 0.35f;
        public float SlowDuration = 1.5f;

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            float damage = GetByRank(BaseDamageByRank, ctx.Rank)
                         + AdRatio * caster.Stats.Value(StatType.AttackDamage);
            var info = new DamageInfo(damage, DamageType.Physical, caster);

            var targets = AbilityUtils.GetEnemiesInRadius(caster, caster.transform.position, Radius, TargetMask);
            foreach (var t in targets)
            {
                t.TakeDamage(info);
                if (AbilityUtils.TryGetChampion(t, out var champ))
                    champ.Status.Apply(new SlowEffect(SlowAmount, SlowDuration));
            }
        }
    }
}
