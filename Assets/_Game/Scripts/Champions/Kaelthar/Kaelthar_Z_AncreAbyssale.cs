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
    /// Z de Kaelthar — Ancre Abyssale.
    /// Projette une ancre dans une direction ; les ennemis touchés sont attirés vers
    /// Kaelthar, subissent des dégâts et sont brièvement immobilisés à l'arrivée.
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Kaelthar/Z - Ancre Abyssale",
                     fileName = "AB_Kaelthar_Z")]
    public sealed class Kaelthar_Z_AncreAbyssale : AbilityData
    {
        [Header("Zone d'accroche")]
        public float CatchRadius = 2.5f;
        public LayerMask TargetMask = ~0;

        [Header("Attraction")]
        public float PullSpeed = 18f;
        public float RootOnArrival = 0.5f;

        [Header("Dégâts par rang")]
        public float[] DamageByRank = { 60f, 90f, 120f, 150f, 180f };
        public float ApRatio = 0.4f;

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            // Point d'impact de l'ancre au bout de la portée, dans la direction visée.
            Vector3 impact = caster.transform.position + ctx.AimDirection * Range;

            float damage = GetByRank(DamageByRank, ctx.Rank)
                         + ApRatio * caster.Stats.Value(StatType.AbilityPower);
            var info = new DamageInfo(damage, DamageType.Magical, caster);

            var hits = AbilityUtils.GetEnemiesInRadius(caster, impact, CatchRadius, TargetMask);
            foreach (var hit in hits)
            {
                hit.TakeDamage(info);
                if (AbilityUtils.TryGetChampion(hit, out var champ))
                {
                    float dist = Vector3.Distance(champ.transform.position, caster.transform.position);
                    champ.Motor.PullToward(caster.transform.position, dist - 1.5f, PullSpeed);
                    champ.Status.Apply(new RootEffect(RootOnArrival));
                }
            }

            // TODO (VFX) : projectile d'ancre + chaîne reliant Kaelthar aux cibles.
        }
    }
}
