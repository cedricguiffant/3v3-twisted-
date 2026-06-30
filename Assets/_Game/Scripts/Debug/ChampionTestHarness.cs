using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Progression;
using Twisted3v3.Economy;

namespace Twisted3v3.DebugTools
{
    /// <summary>
    /// Utilitaire de DEBUG pour tester un champion seul, sans système de niveaux ni
    /// économie. À retirer du build final. Apprend (et monte) les capacités au start,
    /// recharge le mana, et affiche un overlay PV/Mana à l'écran.
    /// </summary>
    [RequireComponent(typeof(Champion))]
    public sealed class ChampionTestHarness : MonoBehaviour
    {
        [SerializeField] private bool _learnAbilitiesOnStart = true;
        [Tooltip("Nombre de niveaux investis dans chaque sort au démarrage.")]
        [SerializeField, Range(1, 5)] private int _abilityRank = 3;
        [SerializeField] private bool _infiniteMana = true;
        [SerializeField] private bool _showOverlay = true;

        private Champion _champion;
        private LevelSystem _levels;
        private GoldWallet _wallet;

        private void Awake()
        {
            _champion = GetComponent<Champion>();
            _levels = GetComponent<LevelSystem>();
            _wallet = GetComponent<GoldWallet>();
        }

        private void Start()
        {
            if (!_learnAbilitiesOnStart) return;
            foreach (var slot in new[] { AbilitySlot.Q, AbilitySlot.Z, AbilitySlot.E, AbilitySlot.R })
                for (int i = 0; i < _abilityRank; i++)
                    _champion.Abilities.LevelUp(slot);
        }

        private void Update()
        {
            if (_infiniteMana) _champion.RefillMana();
        }

        private void OnGUI()
        {
            if (!_showOverlay || _champion.Health == null) return;

            var style = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            string progression = "";
            if (_levels != null)
                progression = $"Niveau {_champion.Level}  XP {_levels.CurrentXp:0}/{_levels.XpToNextLevel:0}" +
                              $"  Points: {_levels.SkillPoints}\n";
            if (_wallet != null)
                progression += $"Or: {_wallet.Gold}\n";

            string text =
                $"{(_champion.Data ? _champion.Data.DisplayName : "Champion")}\n" +
                progression +
                $"PV: {_champion.Health.CurrentHealth:0}/{_champion.Health.MaxHealth:0}" +
                $"  Bouclier: {_champion.Health.Shield:0}\n" +
                $"Mana: {_champion.CurrentMana:0}\n" +
                $"Q {Ready(AbilitySlot.Q)}  Z {Ready(AbilitySlot.Z)}  " +
                $"E {Ready(AbilitySlot.E)}  R {Ready(AbilitySlot.R)}\n" +
                $"(Ctrl+Q/Z/E/R: monter un sort)";
            GUI.Label(new Rect(12, 12, 460, 170), text, style);
        }

        private string Ready(AbilitySlot slot)
        {
            var inst = _champion.Abilities.GetSlot(slot);
            if (inst == null || !inst.IsLearned) return "-";
            return inst.IsReady ? "OK" : $"{inst.CooldownRemaining:0.0}s";
        }
    }
}
