using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Abilities.Effects;
using Twisted3v3.Combat.StatusEffects;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Vexor
{
    /// <summary>
    /// E de Vexor — Frappe du Néant. Frappe en zone autour de lui : dégâts et silence
    /// des ennemis touchés (empêche leurs sorts).
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Vexor/E - Frappe du Néant",
                     fileName = "AB_Vexor_E")]
    public sealed class Vexor_E_FrappeDuNeant : AbilityData
    {
        public float Radius = 3.5f;
        public LayerMask TargetMask = ~0;
        public float[] BaseDamageByRank = { 60f, 90f, 120f, 150f, 180f };
        public float AdRatio = 0.5f;
        public float[] SilenceByRank = { 0.8f, 1f, 1.2f, 1.4f, 1.6f };

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            float damage = GetByRank(BaseDamageByRank, ctx.Rank)
                         + AdRatio * caster.Stats.Value(StatType.AttackDamage);
            float silence = GetByRank(SilenceByRank, ctx.Rank);
            var info = new DamageInfo(damage, DamageType.Magical, caster);

            var targets = AbilityUtils.GetEnemiesInRadius(caster, caster.transform.position, Radius, TargetMask);
            foreach (var t in targets)
            {
                t.TakeDamage(info);
                if (AbilityUtils.TryGetChampion(t, out var champ))
                    champ.Status.Apply(new SilenceEffect(silence));
            }
        }
    }
}
