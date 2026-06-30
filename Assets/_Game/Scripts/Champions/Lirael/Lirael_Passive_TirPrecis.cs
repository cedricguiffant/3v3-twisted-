using UnityEngine;
using Twisted3v3.Abilities;
using Twisted3v3.Champions;

namespace Twisted3v3.Champions.Lirael
{
    /// <summary>
    /// Passif de Lirael — Tir Précis. Chaque auto-attaque accumule des charges qui
    /// augmentent sa vitesse d'attaque et sa portée (jusqu'à un maximum). Branché via
    /// OnLearned (état + écoute des attaques dans un composant runtime).
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Lirael/Passive - Tir Précis",
                     fileName = "AB_Lirael_Passive")]
    public sealed class Lirael_Passive_TirPrecis : AbilityData
    {
        [Header("Charges")]
        public int MaxStacks = 8;
        public float AttackSpeedPerStack = 0.04f; // +4% AS par charge
        public float RangePerStack = 0.15f;

        public override void Execute(AbilityContext context) { }

        public override void OnLearned(Champion champion)
        {
            var rt = champion.GetComponent<TirPrecisRuntime>()
                     ?? champion.gameObject.AddComponent<TirPrecisRuntime>();
            rt.Initialize(champion, this);
        }

        public override void OnUnlearned(Champion champion)
        {
            if (champion.TryGetComponent<TirPrecisRuntime>(out var rt)) rt.Teardown();
        }
    }
}
