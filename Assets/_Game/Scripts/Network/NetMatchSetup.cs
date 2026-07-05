using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Twisted3v3.AI;
using Twisted3v3.CameraControl;
using Twisted3v3.Champions;
using Twisted3v3.Combat;
using Twisted3v3.Jungle;
using Twisted3v3.Minions;
using Twisted3v3.Player;
using Twisted3v3.UI;

namespace Twisted3v3.Net
{
    /// <summary>
    /// Réglages de la scène de match en multijoueur. Les sbires et la jungle ne
    /// sont pas synchronisés (v1) : le mode réseau est une arène champions +
    /// tours/Nexus, appliquée à l'identique chez l'hôte et les clients pour que
    /// les deux mondes restent cohérents.
    /// </summary>
    public static class NetMatchSetup
    {
        /// <summary>Champs du PlayerController repris par le contrôleur réseau.</summary>
        private static readonly string[] CopiedFields =
        {
            "_groundMask", "_unitMask", "_maxRayDistance",
            "_qKey", "_zKey", "_eKey", "_rKey", "_attackMoveKey"
        };

        /// <summary>Règles d'arène : coupe ce qui n'est pas synchronisé en réseau.</summary>
        public static void ApplyArenaRules()
        {
            foreach (var spawner in Object.FindObjectsByType<MinionWaveSpawner>(FindObjectsSortMode.None))
                Object.Destroy(spawner);
            foreach (var camp in Object.FindObjectsByType<JungleCamp>(FindObjectsSortMode.None))
                Object.Destroy(camp);
            foreach (var altar in Object.FindObjectsByType<SoulAltar>(FindObjectsSortMode.None))
                Object.Destroy(altar);
            foreach (var monster in Object.FindObjectsByType<JungleMonster>(FindObjectsSortMode.None))
                Object.Destroy(monster.gameObject);
        }

        /// <summary>
        /// Côté client uniquement : retire toute la simulation locale de contrôle
        /// (IA, PlayerController, respawn, tir des tours) — l'hôte fait foi.
        /// Renvoie les réglages capturés du PlayerController (masques, touches)
        /// pour le <see cref="NetClientController"/>.
        /// </summary>
        public static Dictionary<string, object> StripLocalControl(List<Champion> champions)
        {
            Dictionary<string, object> settings = null;

            foreach (var c in champions)
            {
                var pc = c.GetComponent<PlayerController>();
                if (pc != null)
                {
                    settings ??= Capture(pc);
                    Object.DestroyImmediate(pc);
                }
                foreach (var ai in c.GetComponents<ChampionAI>())
                    Object.DestroyImmediate(ai);
                var harness = c.GetComponent<DebugTools.ChampionTestHarness>();
                if (harness != null) Object.DestroyImmediate(harness);
                var respawn = c.GetComponent<RespawnController>();
                if (respawn != null) Object.Destroy(respawn);
            }

            foreach (var weapon in Object.FindObjectsByType<TowerWeapon>(FindObjectsSortMode.None))
                Object.Destroy(weapon);

            return settings;
        }

        /// <summary>Caméra, HUD, barre de sorts et vision suivent le champion local.</summary>
        public static void BindUi(Champion player)
        {
            var camera = Object.FindFirstObjectByType<MobaCamera>();
            if (camera != null) camera.SetTarget(player.transform);
            Object.FindFirstObjectByType<HUDController>()?.Bind(player);
            Object.FindFirstObjectByType<AbilityBarUI>()?.Bind(player);
            Object.FindFirstObjectByType<Vision.VisionSystem>()?.SetViewer(player);
        }

        private static Dictionary<string, object> Capture(PlayerController template)
        {
            var values = new Dictionary<string, object>();
            var type = typeof(PlayerController);
            foreach (var name in CopiedFields)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null) values[name] = field.GetValue(template);
            }
            return values;
        }
    }
}
