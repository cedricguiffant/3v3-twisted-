using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Abilities.Effects;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Vexor
{
    /// <summary>
    /// R (Ultime) de Vexor — Exécution des Ombres. Vexor se fond dans l'ombre (bref
    /// bouclier, proxy d'intangibilité), bondit sur sa cible et inflige un burst ;
    /// les dégâts sont amplifiés si la cible est sous un seuil de PV (exécution).
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Vexor/R - Exécution des Ombres",
                     fileName = "AB_Vexor_R")]
    public sealed class Vexor_R_ExecutionDesOmbres : AbilityData
    {
        [Header("Bond")]
        public float BlinkSpeed = 30f;
        public float SearchRadius = 6f;
        public LayerMask TargetMask = ~0;

        [Header("Burst par rang (ultime : 3 rangs)")]
        public float[] BaseDamageByRank = { 160f, 260f, 360f };
        public float AdRatio = 1.2f;

        [Header("Exécution")]
        [Range(0f, 1f)] public float ExecuteThreshold = 0.30f;
        public float ExecuteMultiplier = 2f;

        [Header("Intangibilité (proxy)")]
        public float Shield = 150f;
        public float ShieldDuration = 1.5f;

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;

            // Cible : unité visée si ennemie, sinon la plus proche dans la direction.
            IDamageable target = ctx.TargetUnit;
            if (target == null || target.IsDead || !caster.IsEnemy(target))
            {
                Vector3 probe = caster.transform.position + ctx.AimDirection * SearchRadius;
                var list = AbilityUtils.GetEnemiesInRadius(caster, probe, SearchRadius, TargetMask);
                target = list.Count > 0 ? list[0] : null;
            }

            // Intangibilité simplifiée : bouclier bref.
            caster.Health.AddShield(Shield, ShieldDuration);
            if (target == null) return;

            // Bond derrière/sur la cible.
            Vector3 dir = target.Transform.position - caster.transform.position; dir.y = 0f;
            Vector3 dest = target.Transform.position - dir.normalized * 1.2f;
            caster.Motor.Dash(dest, BlinkSpeed);

            // Burst + exécution.
            float dmg = GetByRank(BaseDamageByRank, ctx.Rank)
                      + AdRatio * caster.Stats.Value(StatType.AttackDamage);
            if (target is IHealthInfo hp && hp.MaxHealth > 0f
                && hp.CurrentHealth / hp.MaxHealth < ExecuteThreshold)
                dmg *= ExecuteMultiplier;

            target.TakeDamage(new DamageInfo(dmg, DamageType.Physical, caster));
        }
    }
}
