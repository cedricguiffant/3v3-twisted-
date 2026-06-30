using UnityEngine;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Lirael
{
    /// <summary>
    /// E de Lirael — Piège de Lumière. Pose un piège au point visé : il s'arme, puis
    /// immobilise et blesse le premier ennemi qui marche dessus.
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Lirael/E - Piège de Lumière",
                     fileName = "AB_Lirael_E")]
    public sealed class Lirael_E_PiegeDeLumiere : AbilityData
    {
        [Header("Piège")]
        public float TrapRadius = 1.8f;
        public float ArmDelay = 0.75f;
        public float Lifetime = 30f;
        public LayerMask TargetMask = ~0;

        [Header("Effet par rang")]
        public float[] DamageByRank = { 60f, 95f, 130f, 165f, 200f };
        public float ApRatio = 0.4f;
        public float[] RootDurationByRank = { 1f, 1.25f, 1.5f, 1.75f, 2f };

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            float damage = GetByRank(DamageByRank, ctx.Rank)
                         + ApRatio * caster.Stats.Value(StatType.AbilityPower);
            float root = GetByRank(RootDurationByRank, ctx.Rank);

            TrapZone.Spawn(caster, ctx.GroundPoint, TrapRadius, ArmDelay, Lifetime,
                           damage, root, TargetMask, new Color(1f, 0.95f, 0.5f));
        }
    }
}
