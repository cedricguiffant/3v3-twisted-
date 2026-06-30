using UnityEngine;
using Twisted3v3.Abilities;
using Twisted3v3.Champions;

namespace Twisted3v3.Champions.Kaelthar
{
    /// <summary>
    /// Passif de Kaelthar — Âme Fracturée.
    /// Octroie un bouclier chaque fois que Kaelthar a cumulé 25% de ses PV max en
    /// dégâts subis. Un passif ne se "cast" pas : Execute est vide. Le comportement
    /// (qui a besoin d'un état runtime) est branché dans OnLearned via un composant.
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Kaelthar/Passive - Âme Fracturée",
                     fileName = "AB_Kaelthar_Passive")]
    public sealed class Kaelthar_Passive_AmeFracturee : AbilityData
    {
        [Header("Seuil")]
        [Range(0.05f, 1f)] public float HealthLostThreshold = 0.25f; // 25%

        [Header("Bouclier généré")]
        public float BaseShield = 40f;
        public float ShieldPerLevel = 12f;
        [Tooltip("Part des PV max ajoutée au bouclier.")]
        public float MaxHealthRatio = 0.05f;
        public float ShieldDuration = 4f;

        // Les passifs ne se lancent pas.
        public override void Execute(AbilityContext context) { }

        public override void OnLearned(Champion champion)
        {
            // Ajoute le composant runtime qui portera l'état (dégâts cumulés).
            var runtime = champion.gameObject.GetComponent<AmeFractureeRuntime>()
                          ?? champion.gameObject.AddComponent<AmeFractureeRuntime>();
            runtime.Initialize(champion, this);
        }

        public override void OnUnlearned(Champion champion)
        {
            if (champion.TryGetComponent<AmeFractureeRuntime>(out var runtime))
                runtime.Teardown();
        }
    }
}
