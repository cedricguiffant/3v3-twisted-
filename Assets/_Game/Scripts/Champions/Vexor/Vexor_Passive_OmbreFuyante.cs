using UnityEngine;
using Twisted3v3.Abilities;
using Twisted3v3.Champions;

namespace Twisted3v3.Champions.Vexor
{
    /// <summary>
    /// Passif de Vexor — Ombre Fuyante. Quand ses PV tombent sous un seuil, il se fond
    /// dans l'ombre : bond de vitesse + bouclier bref pour fuir (récurrence limitée).
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Vexor/Passive - Ombre Fuyante",
                     fileName = "AB_Vexor_Passive")]
    public sealed class Vexor_Passive_OmbreFuyante : AbilityData
    {
        [Header("Déclenchement")]
        [Range(0.05f, 0.6f)] public float HealthThreshold = 0.30f;
        public float Cooldown = 18f;

        [Header("Effet de fuite")]
        public float MoveSpeedBonus = 0.4f; // +40%
        public float Duration = 2f;
        public float Shield = 120f;

        public override void Execute(AbilityContext context) { }

        public override void OnLearned(Champion champion)
        {
            var rt = champion.GetComponent<OmbreFuyanteRuntime>()
                     ?? champion.gameObject.AddComponent<OmbreFuyanteRuntime>();
            rt.Initialize(champion, this);
        }

        public override void OnUnlearned(Champion champion)
        {
            if (champion.TryGetComponent<OmbreFuyanteRuntime>(out var rt)) rt.Teardown();
        }
    }
}
