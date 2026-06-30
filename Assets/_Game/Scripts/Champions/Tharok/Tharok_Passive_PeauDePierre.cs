using UnityEngine;
using Twisted3v3.Abilities;
using Twisted3v3.Champions;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions.Tharok
{
    /// <summary>
    /// Passif de Tharok — Peau de Pierre. Octroie en permanence une réduction des
    /// dégâts via un bonus d'armure et de résistance magique (appliqué à l'apprentissage).
    /// </summary>
    [CreateAssetMenu(menuName = "Twisted3v3/Abilities/Tharok/Passive - Peau de Pierre",
                     fileName = "AB_Tharok_Passive")]
    public sealed class Tharok_Passive_PeauDePierre : AbilityData
    {
        public float BonusArmor = 25f;
        public float BonusMagicResist = 20f;

        public override void Execute(AbilityContext context) { }

        public override void OnLearned(Champion champion)
        {
            champion.Stats.AddModifier(StatType.Armor, new StatModifier(BonusArmor, ModifierType.Flat, this));
            champion.Stats.AddModifier(StatType.MagicResist, new StatModifier(BonusMagicResist, ModifierType.Flat, this));
        }
    }
}
