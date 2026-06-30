using UnityEngine;

namespace Twisted3v3.Abilities
{
    /// <summary>
    /// État RUNTIME d'une capacité pour un champion donné : cooldown courant,
    /// rang, charges. Une instance par slot par champion. Référence l'asset
    /// partagé <see cref="AbilityData"/> mais ne le modifie jamais.
    /// </summary>
    public sealed class AbilityInstance
    {
        public readonly AbilityData Data;

        public int Rank { get; private set; }
        public float CooldownRemaining { get; private set; }
        public bool IsReady => CooldownRemaining <= 0f && Rank > 0;
        public bool IsLearned => Rank > 0;

        /// <summary>0..1 — utile pour l'overlay de cooldown sur l'UI.</summary>
        public float CooldownPercent
        {
            get
            {
                float total = Data.GetCooldown(Mathf.Max(1, Rank));
                return total > 0f ? Mathf.Clamp01(CooldownRemaining / total) : 0f;
            }
        }

        public AbilityInstance(AbilityData data, int startingRank = 0)
        {
            Data = data;
            Rank = startingRank;
        }

        public void Tick(float deltaTime, float cooldownReduction)
        {
            if (CooldownRemaining <= 0f) return;
            // CDR accélère le retour du sort.
            CooldownRemaining -= deltaTime / Mathf.Max(0.01f, 1f - Mathf.Clamp01(cooldownReduction));
            if (CooldownRemaining < 0f) CooldownRemaining = 0f;
        }

        public bool TryLevelUp()
        {
            if (Rank >= Data.MaxRank) return false;
            Rank++;
            return true;
        }

        public void StartCooldown() => CooldownRemaining = Data.GetCooldown(Mathf.Max(1, Rank));

        /// <summary>Réduit le cooldown courant (passifs de reset, items, etc.).</summary>
        public void ReduceCooldown(float seconds) =>
            CooldownRemaining = Mathf.Max(0f, CooldownRemaining - seconds);

        public void ResetCooldown() => CooldownRemaining = 0f;
    }
}
