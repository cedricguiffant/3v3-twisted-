using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Twisted3v3.AI;
using Twisted3v3.CameraControl;
using Twisted3v3.Champions;
using Twisted3v3.Combat;
using Twisted3v3.Core;
using Twisted3v3.UI;

namespace Twisted3v3.Player
{
    /// <summary>
    /// Donne le contrôle joueur au champion choisi à l'écran de sélection
    /// (<see cref="GameConfig.SelectedChampion"/>) au chargement de la map :
    /// l'élu perd son IA et reçoit un <see cref="PlayerController"/> (réglages copiés
    /// de l'ancien joueur), l'ancien joueur devient IA, et caméra/HUD/barre de sorts
    /// sont rebranchés. S'exécute entre Awake et Start (sceneLoaded) — les UI qui
    /// résolvent leur champion dans Start voient donc le bon. Entièrement runtime.
    /// </summary>
    public static class PlayerChampionBinder
    {
        // Champs du PlayerController à transférer (réglages de scène : layers, touches).
        private static readonly string[] CopiedFields =
        {
            "_groundMask", "_unitMask", "_maxRayDistance",
            "_qKey", "_zKey", "_eKey", "_rKey", "_attackMoveKey"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            Apply();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply();

        private static void Apply()
        {
            string wanted = GameConfig.SelectedChampion;
            if (string.IsNullOrWhiteSpace(wanted)) return;

            var champions = Object.FindObjectsByType<Champion>(FindObjectsSortMode.None);
            if (champions.Length == 0) return; // pas une scène de match

            Champion chosen = null;
            foreach (var c in champions)
                if (Matches(c, wanted)) { chosen = c; break; }

            if (chosen == null)
            {
                Debug.LogWarning($"[ChampionSelect] « {wanted} » introuvable en scène — joueur par défaut conservé.");
                foreach (var c in champions)
                    if (c.GetComponent<PlayerController>() != null) { BindUi(c); break; }
                return;
            }

            // 1) Capture les réglages du premier contrôleur trouvé (layers, touches),
            //    puis convertit en bot TOUT champion non choisi qui porte un contrôleur
            //    (la scène peut en contenir plusieurs — jamais deux joueurs).
            //    DestroyImmediate : on est entre Awake et Start, il ne faut pas que le
            //    Start des composants retirés s'exécute.
            Dictionary<string, object> settings = null;
            foreach (var c in champions)
            {
                if (c == chosen) continue;
                var otherPc = c.GetComponent<PlayerController>();
                if (otherPc != null)
                {
                    settings ??= CaptureSettings(otherPc);
                    Object.DestroyImmediate(otherPc);
                }
                var harness = c.GetComponent<DebugTools.ChampionTestHarness>();
                if (harness != null) Object.DestroyImmediate(harness);
                if (c.GetComponent<ChampionAI>() == null)
                    c.gameObject.AddComponent<ChampionAI>();
            }

            // 2) L'élu devient le joueur.
            foreach (var ai in chosen.GetComponents<ChampionAI>())
                Object.DestroyImmediate(ai);
            var pc = chosen.GetComponent<PlayerController>();
            bool hadController = pc != null;
            if (pc == null) pc = chosen.gameObject.AddComponent<PlayerController>();
            if (!hadController) ApplySettings(pc, settings);

            // Le kiting est un comportement d'IA — jamais pour le joueur.
            var autoAttack = chosen.GetComponent<AutoAttack>();
            if (autoAttack != null) autoAttack.Kite = false;

            BindUi(chosen);
            Debug.Log($"[ChampionSelect] Joueur → {chosen.name} ({chosen.Team}) ; les autres champions sont en IA.");
        }

        private static bool Matches(Champion c, string wanted) =>
            (c.Data != null && string.Equals(c.Data.DisplayName, wanted, System.StringComparison.OrdinalIgnoreCase))
            || c.name.ToLowerInvariant().Contains(wanted.ToLowerInvariant());

        /// <summary>Caméra, HUD et barre de sorts suivent le champion du joueur.</summary>
        private static void BindUi(Champion player)
        {
            var camera = Object.FindFirstObjectByType<MobaCamera>();
            if (camera != null) camera.SetTarget(player.transform);
            Object.FindFirstObjectByType<HUDController>()?.Bind(player);
            Object.FindFirstObjectByType<AbilityBarUI>()?.Bind(player);
        }

        // --------------------------------------------- Transfert des réglages du PC
        private static Dictionary<string, object> CaptureSettings(PlayerController template)
        {
            if (template == null) return null;
            var values = new Dictionary<string, object>();
            var type = typeof(PlayerController);
            foreach (var name in CopiedFields)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null) values[name] = field.GetValue(template);
            }
            return values;
        }

        private static void ApplySettings(PlayerController pc, Dictionary<string, object> settings)
        {
            var type = typeof(PlayerController);
            if (settings != null)
            {
                foreach (var (name, value) in settings)
                    type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(pc, value);
                return;
            }

            // Pas de modèle (aucun joueur pré-existant) : masques déduits des layers projet.
            type.GetField("_groundMask", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(pc, (LayerMask)LayerMask.GetMask("Ground"));
            type.GetField("_unitMask", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(pc, (LayerMask)LayerMask.GetMask("Units"));
        }
    }
}
