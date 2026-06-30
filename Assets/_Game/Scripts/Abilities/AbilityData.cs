using UnityEngine;
using Twisted3v3.Core;

namespace Twisted3v3.Abilities
{
    /// <summary>
    /// ScriptableObject abstrait : données + comportement d'une capacité.
    /// Chaque sort concret hérite et implémente <see cref="Execute"/>.
    /// Les SO sont des assets PARTAGÉS : ne JAMAIS y stocker d'état runtime
    /// (cooldown courant, etc.) — ça vit dans <see cref="AbilityInstance"/>.
    /// </summary>
    public abstract class AbilityData : ScriptableObject
    {
        [Header("Identité")]
        public string DisplayName = "Nouvelle Capacité";
        [TextArea] public string Description;
        public Sprite Icon;
        public AbilitySlot Slot = AbilitySlot.Q;

        [Header("Ciblage")]
        public TargetingType Targeting = TargetingType.Direction;
        public float Range = 8f;
        [Tooltip("Rang maximum : 5 pour Q/Z/E, 3 pour l'ultime (R).")]
        public int MaxRank = 5;

        [Header("Coûts & Cooldown (par rang)")]
        [Tooltip("Cooldown en secondes pour chaque rang. Indexé [rang-1].")]
        public float[] CooldownByRank = { 8f, 7.5f, 7f, 6.5f, 6f };
        [Tooltip("Coût en mana pour chaque rang.")]
        public float[] ManaCostByRank = { 50f, 55f, 60f, 65f, 70f };

        [Header("Cast")]
        [Tooltip("Délai d'animation avant que l'effet ne se déclenche.")]
        public float CastTime = 0.1f;
        public bool CanMoveWhileCasting = false;

        public virtual float GetCooldown(int rank) => GetByRank(CooldownByRank, rank);
        public virtual float GetManaCost(int rank) => GetByRank(ManaCostByRank, rank);

        /// <summary>
        /// Cœur de la capacité. Appelée par l'<see cref="AbilitySystem"/> après
        /// validation (cooldown, mana, ciblage). Implémente ici l'effet concret :
        /// spawn de projectile, dégâts de zone, buff, dash, etc.
        /// </summary>
        public abstract void Execute(AbilityContext context);

        /// <summary>Validation optionnelle additionnelle (ex: cible valide, charges).</summary>
        public virtual bool CanCast(AbilityContext context) => true;

        /// <summary>
        /// Appelé quand la capacité est apprise (rang 0 → 1). Pour les PASSIFS,
        /// c'est ici qu'on branche le comportement permanent — typiquement en
        /// ajoutant un composant runtime au champion (l'état vit dans ce composant,
        /// jamais dans ce SO partagé). Execute reste vide pour un passif.
        /// </summary>
        public virtual void OnLearned(Champions.Champion champion) { }

        /// <summary>Appelé quand la capacité est désapprise (mort définitive, swap). Nettoyage.</summary>
        public virtual void OnUnlearned(Champions.Champion champion) { }

        protected static float GetByRank(float[] array, int rank)
        {
            if (array == null || array.Length == 0) return 0f;
            int index = Mathf.Clamp(rank - 1, 0, array.Length - 1);
            return array[index];
        }
    }
}
