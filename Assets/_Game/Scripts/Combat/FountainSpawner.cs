using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Twisted3v3.Champions;

namespace Twisted3v3.Combat
{
    /// <summary>
    /// Place automatiquement chaque champion dans la fontaine de son équipe au
    /// chargement de toute scène qui en contient une (spawn ET respawn = base).
    /// Les champions d'une même équipe sont répartis en éventail dans le rayon de la
    /// fontaine, face au centre de la map. Entièrement runtime et auto-bootstrapé
    /// (aucun objet à poser en scène, aucune action éditeur requise).
    /// </summary>
    public static class FountainSpawner
    {
        // Espacement latéral entre deux champions d'une même équipe.
        private const float SlotSpacing = 3f;
        // Décalage vers le centre de la map depuis le point de fontaine.
        private const float ForwardOffset = 2.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Couvre la scène déjà ouverte au lancement (Play direct)...
            PlaceAll();
            // ...et toutes les scènes chargées ensuite (menu → Map_3v3).
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => PlaceAll();

        private static void PlaceAll()
        {
            var fountains = Object.FindObjectsByType<FountainZone>(FindObjectsSortMode.None);
            if (fountains.Length == 0) return; // pas une scène de match

            var champions = Object.FindObjectsByType<Champion>(FindObjectsSortMode.None);
            if (champions.Length == 0) return;

            foreach (var fountain in fountains)
            {
                // Champions de l'équipe de cette fontaine, ordre stable (par nom).
                var team = new List<Champion>();
                foreach (var c in champions)
                    if (c != null && c.Team == fountain.Team) team.Add(c);
                if (team.Count == 0) continue;
                team.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

                Vector3 center = fountain.transform.position;
                Vector3 toMid = -center; toMid.y = 0f; // la fontaine est excentrée → viser l'origine
                toMid = toMid.sqrMagnitude > 0.01f ? toMid.normalized : Vector3.forward;
                Vector3 right = Vector3.Cross(Vector3.up, toMid);

                for (int i = 0; i < team.Count; i++)
                {
                    var champ = team[i];
                    float lateral = (i - (team.Count - 1) * 0.5f) * SlotSpacing;
                    Vector3 pos = center + toMid * ForwardOffset + right * lateral;
                    // Jamais sous la hauteur de la fontaine (prefabs à racine y=0).
                    pos.y = Mathf.Max(champ.transform.position.y, center.y);

                    // Déplacement NavMesh-safe (agent), sinon transform direct.
                    if (champ.Motor != null) champ.Motor.Warp(pos);
                    else champ.transform.position = pos;
                    champ.transform.rotation = Quaternion.LookRotation(toMid);

                    // Respawn = fontaine (prioritaire sur un éventuel _spawnPoint sérialisé).
                    if (champ.TryGetComponent<RespawnController>(out var rc))
                        rc.SetSpawnPosition(pos);
                }
            }
        }
    }
}
