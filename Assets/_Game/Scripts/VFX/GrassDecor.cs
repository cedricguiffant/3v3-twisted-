using UnityEngine;
using UnityEngine.SceneManagement;

namespace Twisted3v3.VFX
{
    /// <summary>
    /// Parsème la jungle de petites touffes d'herbe décoratives au chargement de la
    /// map (en plus des hautes herbes des buissons). Déterministe, léger (mesh et
    /// matériau partagés), évite l'Autel et les camps. Entièrement runtime.
    /// </summary>
    public static class GrassDecor
    {
        private const int TuftCount = 90;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            Scatter();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Scatter();

        private static void Scatter()
        {
            var jungle = GameObject.Find("Jungle");
            if (jungle == null || !jungle.TryGetComponent<Renderer>(out var rend)) return;

            var camps = Object.FindObjectsByType<Twisted3v3.Jungle.JungleCamp>(FindObjectsSortMode.None);
            var bounds = rend.bounds;
            var rng = new System.Random(42);
            var parent = new GameObject("GrassDecor").transform;

            for (int i = 0; i < TuftCount; i++)
            {
                var pos = new Vector3(
                    Mathf.Lerp(bounds.min.x + 1f, bounds.max.x - 1f, (float)rng.NextDouble()),
                    0f,
                    Mathf.Lerp(bounds.min.z + 1f, bounds.max.z - 1f, (float)rng.NextDouble()));

                if (pos.sqrMagnitude < 22f) continue; // Autel des Âmes au centre
                bool nearCamp = false;
                foreach (var camp in camps)
                {
                    Vector3 d = camp.transform.position - pos; d.y = 0f;
                    if (d.sqrMagnitude < 9f) { nearCamp = true; break; }
                }
                if (nearCamp) continue;

                float height = 0.35f + (float)rng.NextDouble() * 0.4f; // basse : décor, pas cachette
                float g = 0.75f + (float)rng.NextDouble() * 0.4f;
                Grass.CreateTuft(pos, height, new Color(g * 0.9f, g, g * 0.85f),
                    parent, (float)rng.NextDouble());
            }
        }
    }
}
