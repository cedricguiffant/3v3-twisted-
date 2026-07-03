using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Abilities.Effects;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Vexor
{
    /// <summary>
    /// Q de Vexor — Lame Fantôme. Lance une lame qui blesse le premier ennemi touché
    /// et le marque (ralentissement) pour faciliter le suivi.
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Vexor/Q - Lame Fantôme",
                     fileName = "AB_Vexor_Q")]
    public sealed class Vexor_Q_LameFantome : AbilityData
    {
        public float ProjectileSpeed = 26f;
        public float ProjectileRadius = 0.45f;
        public LayerMask TargetMask = ~0;
        public float[] BaseDamageByRank = { 65f, 100f, 135f, 170f, 205f };
        public float AdRatio = 0.7f;
        [Range(0f, 1f)] public float SlowAmount = 0.3f;
        public float SlowDuration = 1.5f;

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            float damage = GetByRank(BaseDamageByRank, ctx.Rank)
                         + AdRatio * caster.Stats.Value(StatType.AttackDamage);
            Vector3 origin = caster.transform.position + Vector3.up + ctx.AimDirection * 0.6f;

            // Effet composable : ralentissement à l'impact (cf. AbilityEffects).
            System.Action<IDamageable> onHit = victim => AbilityEffects.Slow(victim, SlowAmount, SlowDuration);

            Projectile.Spawn(caster, origin, ctx.AimDirection, ProjectileSpeed, Range,
                ProjectileRadius, damage, DamageType.Physical, TargetMask,
                pierce: false, homing: null, onHit: onHit, color: new Color(0.5f, 0.3f, 0.7f));
        }
    }
}
