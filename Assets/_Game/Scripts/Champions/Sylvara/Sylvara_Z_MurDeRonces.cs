using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Sylvara
{
    /// <summary>
    /// Z de Sylvara — Mur de Ronces. Crée une zone de ronces au sol qui blesse et
    /// ralentit les ennemis qui s'y trouvent, pendant une durée.
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Sylvara/Z - Mur de Ronces",
                     fileName = "AB_Sylvara_Z")]
    public sealed class Sylvara_Z_MurDeRonces : AbilityData
    {
        public float ZoneRadius = 3f;
        public float Duration = 4f;
        public float TickInterval = 0.5f;
        public LayerMask TargetMask = ~0;
        [Range(0f, 1f)] public float SlowAmount = 0.4f;
        public float[] DamagePerTickByRank = { 20f, 30f, 40f, 50f, 60f };
        public float ApRatio = 0.15f;

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            float tick = GetByRank(DamagePerTickByRank, ctx.Rank)
                       + ApRatio * caster.Stats.Value(StatType.AbilityPower);
            AuraZone.Spawn(caster, ctx.GroundPoint, ZoneRadius, tick, 0f, TickInterval,
                Duration, SlowAmount, TickInterval + 0.1f, TargetMask,
                new Color(0.35f, 0.6f, 0.25f), follow: false);
        }
    }
}
