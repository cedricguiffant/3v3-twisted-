using UnityEngine;
using Twisted3v3.Abilities;
using Twisted3v3.Champions;
using Twisted3v3.Combat;

namespace Twisted3v3.Champions.Sylvara
{
    /// <summary>
    /// Passif de Sylvara — Épines Vivantes. Une aura d'épines l'entoure en permanence
    /// et inflige des dégâts continus aux ennemis proches. (Spawn d'une AuraZone qui
    /// suit Sylvara, dégât sur la durée.)
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Sylvara/Passive - Épines Vivantes",
                     fileName = "AB_Sylvara_Passive")]
    public sealed class Sylvara_Passive_EpinesVivantes : AbilityData
    {
        [Header("Aura d'épines")]
        public float AuraRadius = 3.5f;
        public float DamagePerTick = 10f;
        public float TickInterval = 0.5f;
        public LayerMask TargetMask = ~0;

        public override void Execute(AbilityContext context) { }

        public override void OnLearned(Champion champion)
        {
            AuraZone.Spawn(champion, champion.transform.position, AuraRadius,
                DamagePerTick, 0f, TickInterval, 0f /*infini*/, 0f, 0f,
                TargetMask, new Color(0.3f, 0.7f, 0.3f), follow: true);
        }
    }
}
