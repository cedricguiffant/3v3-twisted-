using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Twisted3v3.Champions;

namespace Twisted3v3.Net
{
    /// <summary>
    /// Point d'entrée réseau : singleton persistant entre les scènes qui héberge
    /// le <see cref="NetServer"/> (hôte) ou le <see cref="NetClient"/> et les
    /// pompe à chaque frame. Configure la scène de match une frame après son
    /// chargement (donc après le PlayerChampionBinder), et coupe le réseau au
    /// retour au menu.
    /// </summary>
    public sealed class NetRunner : MonoBehaviour
    {
        public static NetRunner Instance { get; private set; }

        public NetServer Server { get; private set; }
        public NetClient Client { get; private set; }

        public static bool IsHost => Instance != null && Instance.Server != null;
        public static bool IsClient => Instance != null && Instance.Client != null;
        public static bool IsActive => IsHost || IsClient;

        public static NetRunner Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("NetRunner");
                go.AddComponent<NetRunner>(); // Awake assigne Instance
            }
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Shutdown();
            Instance = null;
        }

        private void OnApplicationQuit() => Shutdown();

        public NetServer StartServer(int port)
        {
            Shutdown();
            Server = new NetServer();
            Server.Start(port);
            return Server;
        }

        public NetClient StartClient(string address, int port)
        {
            Shutdown();
            Client = new NetClient();
            Client.Connect(address, port);
            return Client;
        }

        public void Shutdown()
        {
            Server?.Stop();
            Server = null;
            Client?.Stop();
            Client = null;
        }

        private void Update()
        {
            Server?.Tick();
            Client?.Tick();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsActive) return;
            StartCoroutine(SetupNextFrame());
        }

        private IEnumerator SetupNextFrame()
        {
            // Une frame de retard : le PlayerChampionBinder (hôte) et les Awake/Start
            // de la scène sont passés, on peut réorganiser les contrôleurs.
            yield return null;
            if (!IsActive) yield break;

            bool isMatchScene = FindObjectsByType<Champion>(FindObjectsSortMode.None).Length > 0;
            if (isMatchScene)
            {
                Server?.OnMatchSceneReady();
                Client?.OnMatchSceneReady();
            }
            else
            {
                // Retour au menu après une partie : on coupe le réseau proprement.
                bool matchWasRunning = (Server != null && Server.MatchStarted)
                                       || (Client != null && Client.MatchStarted);
                if (matchWasRunning)
                {
                    Debug.Log("[Net] Retour au menu — arrêt du réseau.");
                    Shutdown();
                }
            }
        }
    }
}
