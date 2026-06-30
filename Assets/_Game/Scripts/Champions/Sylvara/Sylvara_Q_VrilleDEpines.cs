using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Sylvara
{
    /// <summary>
    /// Q de Sylvara — Vrille d'Épines. Projectile végétal qui inflige des dégâts
    /// magiques au premier ennemi touché.
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Sylvara/Q - Vrille d'Épines",
                     fileName = "AB_Sylvara_Q")]
    public sealed class Sylvara_Q_VrilleDEpines : AbilityData
    {
        public float ProjectileSpeed = 20f;
        public float ProjectileRadius = 0.5f;
        public LayerMask TargetMask = ~0;
        public float[] BaseDamageByRank = { 70f, 110f, 150f, 190f, 230f };
        public float ApRatio = 0.7f;

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            float damage = GetByRank(BaseDamageByRank, ctx.Rank)
                         + ApRatio * caster.Stats.Value(StatType.AbilityPower);
            Vector3 origin = caster.transform.position + Vector3.up + ctx.AimDirection * 0.6f;
            Projectile.Spawn(caster, origin, ctx.AimDirection, ProjectileSpeed, Range,
                ProjectileRadius, damage, DamageType.Magical, TargetMask,
                pierce: false, homing: null, onHit: null, color: new Color(0.4f, 0.8f, 0.3f));
        }
    }
}
