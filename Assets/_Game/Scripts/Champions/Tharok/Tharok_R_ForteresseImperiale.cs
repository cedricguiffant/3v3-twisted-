using UnityEngine;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;

namespace Twisted3v3.Champions.Tharok
{
    /// <summary>
    /// R (Ultime) de Tharok — Forteresse Impériale. Établit une vaste zone de
    /// protection : soigne les alliés présents et ralentit les ennemis, pendant une
    /// durée (zone de contrôle défensive d'équipe).
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Tharok/R - Forteresse Impériale",
                     fileName = "AB_Tharok_R")]
    public sealed class Tharok_R_ForteresseImperiale : AbilityData
    {
        public float ZoneRadius = 7f;
        public float Duration = 5f;
        public float TickInterval = 0.5f;
        public LayerMask TargetMask = ~0;
        [Range(0f, 1f)] public float SlowAmount = 0.4f;
        public float[] AllyHealPerTickByRank = { 25f, 40f, 55f };
        public float EnemyDamagePerTick = 10f;

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            float heal = GetByRank(AllyHealPerTickByRank, ctx.Rank);

            AuraZone.Spawn(caster, ctx.GroundPoint, ZoneRadius, EnemyDamagePerTick, heal,
                TickInterval, Duration, SlowAmount, TickInterval + 0.1f, TargetMask,
                new Color(0.55f, 0.6f, 0.8f), follow: false);
        }
    }
}
