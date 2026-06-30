using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Abilities.Effects;
using Twisted3v3.Combat.StatusEffects;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Kaelthar
{
    /// <summary>
    /// R (Ultime) de Kaelthar — Jugement du Vide.
    /// Explosion en zone au point visé : dégâts magiques lourds et étourdissement
    /// de tous les ennemis touchés. Outil de teamfight (3v3 → fort impact).
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Kaelthar/R - Jugement du Vide",
                     fileName = "AB_Kaelthar_R")]
    public sealed class Kaelthar_R_JugementDuVide : AbilityData
    {
        [Header("Explosion")]
        public float ExplosionRadius = 4f;
        public LayerMask TargetMask = ~0;

        [Header("Dégâts par rang (ultime : 3 rangs)")]
        public float[] DamageByRank = { 200f, 325f, 450f };
        public float ApRatio = 0.8f;

        [Header("Étourdissement par rang")]
        public float[] StunByRank = { 1f, 1.25f, 1.5f };

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            Vector3 center = ctx.GroundPoint; // ciblage au sol

            float damage = GetByRank(DamageByRank, ctx.Rank)
                         + ApRatio * caster.Stats.Value(StatType.AbilityPower);
            float stun = GetByRank(StunByRank, ctx.Rank);
            var info = new DamageInfo(damage, DamageType.Magical, caster);

            var targets = AbilityUtils.GetEnemiesInRadius(caster, center, ExplosionRadius, TargetMask);
            foreach (var target in targets)
            {
                target.TakeDamage(info);
                if (AbilityUtils.TryGetChampion(target, out var champ))
                    champ.Status.Apply(new StunEffect(stun));
            }

            // TODO (VFX) : implosion du vide au point ciblé + onde de choc.
        }
    }
}
