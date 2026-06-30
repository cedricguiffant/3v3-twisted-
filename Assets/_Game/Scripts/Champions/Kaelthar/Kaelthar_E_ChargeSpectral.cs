using UnityEngine;
using Twisted3v3.Abilities;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Kaelthar
{
    /// <summary>
    /// E de Kaelthar — Charge Spectral.
    /// Dash dans la direction visée et octroi d'un bouclier temporisé au lanceur.
    /// Outil d'engagement / désengagement.
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Kaelthar/E - Charge Spectral",
                     fileName = "AB_Kaelthar_E")]
    public sealed class Kaelthar_E_ChargeSpectral : AbilityData
    {
        [Header("Dash")]
        public float DashDistance = 5f;
        public float DashSpeed = 22f;

        [Header("Bouclier par rang")]
        public float[] ShieldByRank = { 60f, 95f, 130f, 165f, 200f };
        public float BonusArmorRatio = 0.5f; // bouclier renforcé par l'armure
        public float ShieldDuration = 3f;

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;

            Vector3 destination = caster.transform.position + ctx.AimDirection * DashDistance;
            caster.Motor.Dash(destination, DashSpeed);

            float shield = GetByRank(ShieldByRank, ctx.Rank)
                         + BonusArmorRatio * caster.Stats.Value(StatType.Armor);
            caster.Health.AddShield(shield, ShieldDuration);

            // TODO (VFX) : traînée spectrale pendant le dash.
        }
    }
}
