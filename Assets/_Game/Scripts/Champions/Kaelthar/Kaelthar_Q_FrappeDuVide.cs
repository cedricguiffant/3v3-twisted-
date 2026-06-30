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
    /// Q de Kaelthar — Frappe du Vide.
    /// Cône de dégâts magiques devant le champion + ralentissement temporaire.
    /// EXEMPLE de référence : montre comment un sort concret hérite d'AbilityData,
    /// scale par rang, lit les stats du lanceur et applique un effet de zone.
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Kaelthar/Q - Frappe du Vide",
                     fileName = "AB_Kaelthar_Q")]
    public sealed class Kaelthar_Q_FrappeDuVide : AbilityData
    {
        [Header("Cône")]
        public float ConeAngle = 60f;
        public LayerMask TargetMask = ~0;

        [Header("Dégâts par rang")]
        public float[] BaseDamageByRank = { 70f, 110f, 150f, 190f, 230f };
        [Tooltip("Ratio appliqué à la Puissance (AP) du lanceur.")]
        public float ApRatio = 0.6f;

        [Header("Ralentissement")]
        [Range(0f, 1f)] public float SlowAmount = 0.3f; // 30%
        public float SlowDuration = 1.5f;

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            Vector3 origin = caster.transform.position;
            Vector3 dir = ctx.AimDirection;

            // Dégâts = base(rang) + ratio * AP
            float damage = GetByRank(BaseDamageByRank, ctx.Rank)
                         + ApRatio * caster.Stats.Value(StatType.AbilityPower);

            var targets = AbilityUtils.GetEnemiesInCone(
                caster, origin, dir, Range, ConeAngle, TargetMask);

            var info = new DamageInfo(damage, DamageType.Magical, caster);

            foreach (var target in targets)
            {
                target.TakeDamage(info);
                // Ralentissement appliqué via le StatusEffectController de la cible.
                if (AbilityUtils.TryGetChampion(target, out var champ))
                    champ.Status.Apply(new SlowEffect(SlowAmount, SlowDuration));
            }

            // TODO (couche VFX) : spawn du VFX de cône, son, animation de cast.
        }
    }
}
