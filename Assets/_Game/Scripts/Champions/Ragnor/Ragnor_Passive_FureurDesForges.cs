using UnityEngine;
using Twisted3v3.Abilities;
using Twisted3v3.Champions;

namespace Twisted3v3.Champions.Ragnor
{
    /// <summary>
    /// Passif de Ragnor — Fureur des Forges.
    /// En combat, Ragnor accumule des charges qui augmentent ses dégâts d'attaque ;
    /// les charges retombent après un temps hors combat. Branché via OnLearned (le
    /// comportement et l'état vivent dans un composant runtime).
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Ragnor/Passive - Fureur des Forges",
                     fileName = "AB_Ragnor_Passive")]
    public sealed class Ragnor_Passive_FureurDesForges : AbilityData
    {
        [Header("Charges")]
        public int MaxStacks = 5;
        public float AttackDamagePerStack = 8f;
        [Tooltip("Délai sans subir de dégâts avant la perte des charges.")]
        public float DecayDelay = 5f;

        public override void Execute(AbilityContext context) { }

        public override void OnLearned(Champion champion)
        {
            var runtime = champion.GetComponent<FureurDesForgesRuntime>()
                          ?? champion.gameObject.AddComponent<FureurDesForgesRuntime>();
            runtime.Initialize(champion, this);
        }

        public override void OnUnlearned(Champion champion)
        {
            if (champion.TryGetComponent<FureurDesForgesRuntime>(out var runtime))
                runtime.Teardown();
        }
    }
}
