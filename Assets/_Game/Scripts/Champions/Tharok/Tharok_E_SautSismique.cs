using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Abilities.Effects;
using Twisted3v3.Combat.StatusEffects;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Tharok
{
    /// <summary>
    /// E de Tharok — Saut Sismique. Bondit vers le point visé et, à l'atterrissage,
    /// inflige des dégâts en zone et ralentit les ennemis (engagement / initiation).
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Tharok/E - Saut Sismique",
                     fileName = "AB_Tharok_E")]
    public sealed class Tharok_E_SautSismique : AbilityData
    {
        public float LeapSpeed = 22f;
        public float ImpactRadius = 3.5f;
        public LayerMask TargetMask = ~0;
        public float[] BaseDamageByRank = { 60f, 95f, 130f, 165f, 200f };
        public float AdRatio = 0.5f;
        [Range(0f, 1f)] public float SlowAmount = 0.35f;
        public float SlowDuration = 1.5f;

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            Vector3 landing = ctx.GroundPoint;
            caster.Motor.Dash(landing, LeapSpeed);

            float dmg = GetByRank(BaseDamageByRank, ctx.Rank)
                      + AdRatio * caster.Stats.Value(StatType.AttackDamage);
            var info = new DamageInfo(dmg, DamageType.Physical, caster);

            var targets = AbilityUtils.GetEnemiesInRadius(caster, landing, ImpactRadius, TargetMask);
            foreach (var t in targets)
            {
                t.TakeDamage(info);
                if (AbilityUtils.TryGetChampion(t, out var champ))
                    champ.Status.Apply(new SlowEffect(SlowAmount, SlowDuration));
            }
        }
    }
}
