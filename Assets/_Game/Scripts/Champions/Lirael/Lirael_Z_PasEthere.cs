using UnityEngine;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;

namespace Twisted3v3.Champions.Lirael
{
    /// <summary>
    /// Z de Lirael — Pas Éthéré. Court dash de repositionnement qui réinitialise le
    /// délai d'auto-attaque (« attaque gratuite » juste après le dash).
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Lirael/Z - Pas Éthéré",
                     fileName = "AB_Lirael_Z")]
    public sealed class Lirael_Z_PasEthere : AbilityData
    {
        [Header("Dash")]
        public float DashDistance = 4.5f;
        public float DashSpeed = 26f;

        public override void Execute(AbilityContext ctx)
        {
            var caster = ctx.Caster;
            Vector3 destination = caster.transform.position + ctx.AimDirection * DashDistance;
            caster.Motor.Dash(destination, DashSpeed);

            // Attaque gratuite : la prochaine auto-attaque part immédiatement.
            if (caster.TryGetComponent<AutoAttack>(out var aa)) aa.ResetCooldown();
        }
    }
}
