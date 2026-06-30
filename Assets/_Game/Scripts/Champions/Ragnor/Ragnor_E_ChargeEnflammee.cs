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
    /// E de Ragnor — Charge Enflammée. Ragnor charge dans une direction et étourdit
    /// les ennemis présents à l'arrivée (outil d'engagement).
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Ragnor/E - Charge Enflammée",
                     fileName = "AB_Ragnor_E")]
    public sealed class Ragnor_E_ChargeEnflammee : AbilityData
    {
        [Header("Charge")]
        public float DashDistance = 6f;
        public float DashSpeed = 24f;
        public float ImpactRadius = 2.5f;
        public LayerMask TargetMask = ~0;

        [Header("Dégâts par rang")]
        public float[] BaseDamageByRank = { 50f, 80f, 110f, 140f, 170f };
        public float AdRatio = 0.5f;

        [Header("Étourdissement par rang")]
        public float[] StunByRank = { 0.6f, 0.8f, 1f, 1.2f, 1.4f };

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            Vector3 destination = caster.transform.position + ctx.AimDirection * DashDistance;
            caster.Motor.Dash(destination, DashSpeed);

            float damage = GetByRank(BaseDamageByRank, ctx.Rank)
                         + AdRatio * caster.Stats.Value(StatType.AttackDamage);
            float stun = GetByRank(StunByRank, ctx.Rank);
            var info = new DamageInfo(damage, DamageType.Physical, caster);

            // Impact estimé au point d'arrivée de la charge.
            var targets = AbilityUtils.GetEnemiesInRadius(caster, destination, ImpactRadius, TargetMask);
            foreach (var t in targets)
            {
                t.TakeDamage(info);
                if (AbilityUtils.TryGetChampion(t, out var champ))
                    champ.Status.Apply(new StunEffect(stun));
            }
        }
    }
}
