using UnityEditor;
using UnityEngine;
using Twisted3v3.Vision;

/// <summary>
/// Pose un ensemble de buissons (<see cref="BushZone"/>) dans la scène active, groupés
/// sous un objet « Bushes ». Positions pensées pour la map 3v3 (flancs de lane + jungle
/// centrale). Idempotent : ne recrée pas le groupe s'il existe déjà.
/// Lancer via Tools ▸ Twisted3v3 ▸ Add Bushes To Active Scene.
/// </summary>
public static class Twisted3v3VisionSetup
{
    // (x, z, rayon) — flancs de lane et entrées de jungle.
    private static readonly (float x, float z, float r)[] Spots =
    {
        (-14f,  6f, 4f), (14f,  6f, 4f),   // brush haut de lane
        (-14f, -6f, 4f), (14f, -6f, 4f),   // brush bas de lane
        ( 0f,   9f, 4.5f), (0f, -9f, 4.5f) // jungle centrale (haut/bas)
    };

    [MenuItem("Tools/Twisted3v3/Add Bushes To Active Scene")]
    public static void AddBushes()
    {
        if (GameObject.Find("Bushes") != null)
        {
            Debug.Log("[Twisted3v3] Groupe « Bushes » déjà présent — rien à faire.");
            return;
        }

        var group = new GameObject("Bushes");
        int i = 0;
        foreach (var (x, z, r) in Spots)
        {
            var go = new GameObject($"Bush_{i++}");
            go.transform.SetParent(group.transform, false);
            go.transform.position = new Vector3(x, 0f, z);
            var bush = go.AddComponent<BushZone>();
            var so = new SerializedObject(bush);
            so.FindProperty("_radius").floatValue = r;
            so.ApplyModifiedProperties();
        }

        EditorUtility.SetDirty(group);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log($"[Twisted3v3] {Spots.Length} buissons ajoutés (groupe « Bushes »). Sauvegardez la scène.");
    }
}
