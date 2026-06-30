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
    /// Q de Tharok — Coup de Massue. Frappe en cône devant lui : dégâts et
    /// étourdissement des ennemis touchés.
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Tharok/Q - Coup de Massue",
                     fileName = "AB_Tharok_Q")]
    public sealed class Tharok_Q_CoupDeMassue : AbilityData
    {
        public float ConeAngle = 70f;
        public LayerMask TargetMask = ~0;
        public float[] BaseDamageByRank = { 55f, 85f, 115f, 145f, 175f };
        public float AdRatio = 0.6f;
        public float[] StunByRank = { 0.7f, 0.9f, 1.1f, 1.3f, 1.5f };

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            float dmg = GetByRank(BaseDamageByRank, ctx.Rank)
                      + AdRatio * caster.Stats.Value(StatType.AttackDamage);
            float stun = GetByRank(StunByRank, ctx.Rank);
            var info = new DamageInfo(dmg, DamageType.Physical, caster);

            var targets = AbilityUtils.GetEnemiesInCone(
                caster, caster.transform.position, ctx.AimDirection, Range, ConeAngle, TargetMask);
            foreach (var t in targets)
            {
                t.TakeDamage(info);
                if (AbilityUtils.TryGetChampion(t, out var champ))
                    champ.Status.Apply(new StunEffect(stun));
            }
        }
    }
}
