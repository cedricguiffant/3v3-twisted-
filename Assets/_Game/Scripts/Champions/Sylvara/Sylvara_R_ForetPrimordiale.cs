using UnityEngine;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Sylvara
{
    /// <summary>
    /// R (Ultime) de Sylvara — Forêt Primordiale. Grande zone au sol qui blesse et
    /// ralentit les ennemis tout en soignant les alliés présents, pendant une durée.
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Sylvara/R - Forêt Primordiale",
                     fileName = "AB_Sylvara_R")]
    public sealed class Sylvara_R_ForetPrimordiale : AbilityData
    {
        public float ZoneRadius = 6f;
        public float Duration = 5f;
        public float TickInterval = 0.5f;
        public LayerMask TargetMask = ~0;
        [Range(0f, 1f)] public float SlowAmount = 0.35f;
        public float[] EnemyDamagePerTickByRank = { 30f, 45f, 60f };
        public float[] AllyHealPerTickByRank = { 25f, 40f, 55f };
        public float ApRatio = 0.15f;

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            float dmg = GetByRank(EnemyDamagePerTickByRank, ctx.Rank)
                      + ApRatio * caster.Stats.Value(StatType.AbilityPower);
            float heal = GetByRank(AllyHealPerTickByRank, ctx.Rank);

            AuraZone.Spawn(caster, ctx.GroundPoint, ZoneRadius, dmg, heal, TickInterval,
                Duration, SlowAmount, TickInterval + 0.1f, TargetMask,
                new Color(0.25f, 0.7f, 0.35f), follow: false);
        }
    }
}
