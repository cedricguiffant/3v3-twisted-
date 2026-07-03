using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Twisted3v3.VFX;

/// <summary>
/// Passe artistique du projet, en 3 volets (menu Tools ▸ Twisted3v3) :
///  1. « Integrate Ragnor Model » — remplace la capsule de PF_Ragnor par le modèle
///     FBX (Assets/_Game/Art/Models/Ragnor.fbx), auto-mis à l'échelle et posé au sol.
///  2. « Apply Champion Colors » — matériau signature par champion (couleur + émission)
///     assigné au prefab, + composant AbilityCastVfx teinté.
///  3. « Beautify Map » — matériaux de map (sol, lanes, jungle, murs, bases, autel),
///     lumière directionnelle réglée, ambiance trilight + brouillard.
/// « Full Art Pass » enchaîne les trois. Idempotent : ré-exécutable sans dégât.
/// </summary>
public static class Twisted3v3ArtPass
{
    private const string PrefabRoot = "Assets/_Game/Prefabs/Champions";
    private const string MatRoot = "Assets/_Game/Art/Materials";
    private const string MapMatRoot = "Assets/_Game/Art/Materials/Map";
    private const string RagnorFbx = "Assets/_Game/Art/Models/Ragnor.fbx";

    // Couleur signature de chaque champion (matériau + VFX de cast).
    private static readonly Dictionary<string, Color> ChampionColors = new()
    {
        { "Kaelthar", new Color(0.45f, 0.25f, 0.70f) }, // violet du Vide
        { "Ragnor",   new Color(0.85f, 0.30f, 0.12f) }, // rouge lave
        { "Lirael",   new Color(0.95f, 0.85f, 0.45f) }, // or céleste
        { "Sylvara",  new Color(0.30f, 0.75f, 0.40f) }, // vert sylvestre
        { "Vexor",    new Color(0.50f, 0.20f, 0.60f) }, // pourpre d'ombre
        { "Tharok",   new Color(0.55f, 0.55f, 0.62f) }, // gris de pierre
    };

    [MenuItem("Tools/Twisted3v3/Art/Full Art Pass")]
    public static void FullArtPass()
    {
        IntegrateRagnorModel();
        ApplyChampionColors();
        BeautifyMap();
        Debug.Log("[ArtPass] Passe artistique complète terminée.");
    }

    // ================================================================ 1. RAGNOR
    [MenuItem("Tools/Twisted3v3/Art/Integrate Ragnor Model")]
    public static void IntegrateRagnorModel()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(RagnorFbx);
        if (model == null) { Debug.LogError($"[ArtPass] FBX introuvable : {RagnorFbx}"); return; }

        string prefabPath = $"{PrefabRoot}/PF_Ragnor.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            // Retire un modèle déjà intégré (idempotence).
            var existing = root.transform.Find("Model");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            // Instancie le FBX comme enfant « Model ».
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = "Model";
            instance.transform.SetParent(root.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            // Auto-échelle : hauteur cible ~2.6 unités (bruiser imposant).
            var bounds = ComputeBounds(instance);
            if (bounds.size.y > 0.001f)
            {
                float scale = 2.6f / bounds.size.y;
                instance.transform.localScale = Vector3.one * scale;

                // Pose les pieds au niveau du bas de la capsule (local y = -1).
                bounds = ComputeBounds(instance);
                float feetOffset = root.transform.position.y - 1f - bounds.min.y;
                instance.transform.localPosition += Vector3.up * feetOffset;
            }

            // Masque la capsule d'origine (on garde le collider pour le gameplay).
            if (root.TryGetComponent<MeshRenderer>(out var capsule)) capsule.enabled = false;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"[ArtPass] Modèle Ragnor intégré (échelle {instance.transform.localScale.x:0.###}), capsule masquée.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    /// <summary>
    /// Retire le modèle FBX de PF_Ragnor et réaffiche la capsule d'origine
    /// (revert de « Integrate Ragnor Model » — utile si l'échelle d'import change).
    /// </summary>
    [MenuItem("Tools/Twisted3v3/Art/Revert Ragnor Model (capsule)")]
    public static void RevertRagnorModel()
    {
        string prefabPath = $"{PrefabRoot}/PF_Ragnor.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var model = root.transform.Find("Model");
            if (model != null) Object.DestroyImmediate(model.gameObject);
            if (root.TryGetComponent<MeshRenderer>(out var capsule)) capsule.enabled = true;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log("[ArtPass] PF_Ragnor : modèle retiré, capsule réaffichée (état d'avant).");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    /// <summary>
    /// Répare l'échelle du modèle « RagnorModel » posé sur l'instance de scène
    /// (héritage d'une session antérieure) : hauteur cible ~2.6 u, pieds au sol.
    /// L'échelle d'import du FBX ayant changé, sa taille historique est devenue minuscule.
    /// </summary>
    [MenuItem("Tools/Twisted3v3/Art/Fix Ragnor Model Scale (scene)")]
    public static void FixRagnorModelScale()
    {
        var model = GameObject.Find("RagnorModel");
        if (model == null) { Debug.LogWarning("[ArtPass] RagnorModel introuvable dans la scène."); return; }

        if (ComputeBounds(model).size.y < 0.0001f)
        { Debug.LogWarning("[ArtPass] RagnorModel sans renderer mesuré."); return; }

        // Redresse le modèle : l'axe vertical du FBX ne correspond pas au Y d'Unity
        // (modèle couché). On essaie chaque redressement et on garde le plus haut.
        Quaternion bestRotation = model.transform.localRotation;
        float bestHeight = ComputeBounds(model).size.y;
        foreach (var q in new[]
        {
            Quaternion.identity,
            Quaternion.Euler(-90f, 0f, 0f), Quaternion.Euler(90f, 0f, 0f),
            Quaternion.Euler(0f, 0f, 90f), Quaternion.Euler(0f, 0f, -90f)
        })
        {
            model.transform.localRotation = q;
            float h = ComputeBounds(model).size.y;
            if (h > bestHeight + 0.0001f) { bestHeight = h; bestRotation = q; }
        }
        model.transform.localRotation = bestRotation;

        var bounds = ComputeBounds(model);
        float factor = 2.6f / bounds.size.y;
        Vector3 before = model.transform.localScale;
        model.transform.localScale = before * factor;

        // Pieds posés sur le sol de la map (y=0).
        bounds = ComputeBounds(model);
        model.transform.position += Vector3.up * (0f - bounds.min.y);

        // Matériau texturé : albedo extrait du FBX (Ragnor_Albedo.jpg) dans un
        // matériau dédié — les matériaux internes du FBX ne branchent pas leurs
        // textures embarquées (extraites sans extension → renommées .jpg à la main).
        AssetDatabase.Refresh();
        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/_Game/Art/Models/RagnorTextures/Ragnor_Albedo.jpg");
        Material bodyMat = LoadOrCreateMaterial($"{MatRoot}/M_RagnorModel.mat");
        if (albedo != null)
        {
            bodyMat.mainTexture = albedo;
            bodyMat.color = Color.white;
        }
        else
        {
            // Repli : teinte lave signature (mieux qu'un modèle blanc uni).
            var lava = AssetDatabase.LoadAssetAtPath<Material>($"{MatRoot}/M_PF_Ragnor.mat");
            if (lava != null) bodyMat.CopyPropertiesFromMaterial(lava);
        }
        EditorUtility.SetDirty(bodyMat);
        if (model.TryGetComponent<MeshRenderer>(out var rend))
        {
            var mats = new Material[Mathf.Max(1, rend.sharedMaterials.Length)];
            for (int i = 0; i < mats.Length; i++) mats[i] = bodyMat;
            rend.sharedMaterials = mats;
        }

        // La capsule du parent reste masquée : le modèle est le visuel.
        var parent = model.transform.parent;
        if (parent != null && parent.TryGetComponent<MeshRenderer>(out var capsule))
            capsule.enabled = false;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[ArtPass] RagnorModel : rotation {bestRotation.eulerAngles}, échelle {before.x:0.###} → {model.transform.localScale.x:0.###} (hauteur 2.6 u), pieds au sol, scène sauvée.");
    }

    private static Bounds ComputeBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    // ============================================================== 2. COULEURS
    [MenuItem("Tools/Twisted3v3/Art/Apply Champion Colors")]
    public static void ApplyChampionColors()
    {
        EnsureFolder(MatRoot);
        var log = new StringBuilder("[ArtPass] Couleurs champions :\n");

        foreach (var (name, color) in ChampionColors)
        {
            // Matériau signature (couleur + pointe d'émission).
            var mat = LoadOrCreateMaterial($"{MatRoot}/M_PF_{name}.mat");
            mat.color = color;
            mat.SetFloat("_Glossiness", 0.35f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 0.25f);
            EditorUtility.SetDirty(mat);

            string prefabPath = $"{PrefabRoot}/PF_{name}.prefab";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                // Capsule (si visible) → matériau signature.
                if (root.TryGetComponent<MeshRenderer>(out var rend))
                    rend.sharedMaterial = mat;

                // VFX de cast teinté (ajout si absent).
                if (!root.TryGetComponent<AbilityCastVfx>(out var vfx))
                    vfx = root.AddComponent<AbilityCastVfx>();
                var so = new SerializedObject(vfx);
                so.FindProperty("_color").colorValue = color;
                so.ApplyModifiedProperties();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                log.AppendLine($"  {name} → {ColorUtility.ToHtmlStringRGB(color)} (+AbilityCastVfx)");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        AssetDatabase.SaveAssets();
        Debug.Log(log.ToString());
    }

    // ==================================================================== 3. MAP
    [MenuItem("Tools/Twisted3v3/Art/Beautify Map")]
    public static void BeautifyMap()
    {
        EnsureFolder(MapMatRoot);

        var ground = MapMaterial("M_Map_Ground", new Color(0.24f, 0.30f, 0.22f), 0.12f);
        var lane = MapMaterial("M_Map_Lane", new Color(0.55f, 0.49f, 0.36f), 0.18f);
        var jungle = MapMaterial("M_Map_Jungle", new Color(0.14f, 0.24f, 0.15f), 0.10f);
        var wall = MapMaterial("M_Map_Wall", new Color(0.22f, 0.21f, 0.26f), 0.30f);
        var blue = MapMaterial("M_Map_BaseBlue", new Color(0.12f, 0.20f, 0.36f), 0.35f,
                               new Color(0.10f, 0.30f, 0.85f) * 0.5f);
        var red = MapMaterial("M_Map_BaseRed", new Color(0.36f, 0.12f, 0.12f), 0.35f,
                              new Color(0.85f, 0.15f, 0.10f) * 0.5f);
        var altar = MapMaterial("M_Map_Altar", new Color(0.20f, 0.12f, 0.30f), 0.40f,
                                new Color(0.60f, 0.20f, 1.00f) * 0.7f);

        var log = new StringBuilder("[ArtPass] Map :\n");
        int assigned = 0;
        var unmatched = new List<string>();

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!t.TryGetComponent<MeshRenderer>(out var rend)) continue;
            if (t.GetComponentInParent<Twisted3v3.Champions.Champion>() != null) continue; // pas les champions

            string n = t.name.ToLowerInvariant();
            string pn = t.parent != null ? t.parent.name.ToLowerInvariant() : "";
            Material mat = null;

            if (n.Contains("wall") || n.Contains("mur") || pn.Contains("wall")) mat = wall;
            else if (n.Contains("lane")) mat = lane;
            else if (n.Contains("jungle")) mat = jungle;
            else if (n.Contains("altar") || n.Contains("autel")) mat = altar;
            else if (n.Contains("blue")) mat = blue;
            else if (n.Contains("red")) mat = red;
            else if (n.Contains("ground") || n.Contains("sol")) mat = ground;

            if (mat != null)
            {
                rend.sharedMaterial = mat;
                assigned++;
                log.AppendLine($"  {FullPath(t)} → {mat.name}");
            }
            else unmatched.Add(FullPath(t));
        }

        // Lumière directionnelle : chaude, ombres douces.
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (light.type != LightType.Directional) continue;
            light.color = new Color(1.00f, 0.95f, 0.86f);
            light.intensity = 1.3f;
            light.shadows = LightShadows.Soft;
            log.AppendLine($"  Lumière « {light.name} » réglée (chaude, ombres douces).");
        }

        // Ambiance : trilight, SANS brouillard (retiré à la demande — rendait la map brumeuse).
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.44f, 0.46f, 0.50f);
        RenderSettings.ambientGroundColor = new Color(0.22f, 0.22f, 0.26f);
        RenderSettings.fog = false; // désactive aussi le fog déjà sauvé dans la scène

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        log.AppendLine($"  → {assigned} objets rethémés, ambiance + brouillard appliqués, scène sauvegardée.");
        if (unmatched.Count > 0)
            log.AppendLine($"  Non couverts ({unmatched.Count}) : {string.Join(", ", unmatched)}");
        Debug.Log(log.ToString());
    }

    // ================================================================== HELPERS
    private static Material MapMaterial(string name, Color color, float smoothness,
                                        Color? emission = null)
    {
        var mat = LoadOrCreateMaterial($"{MapMatRoot}/{name}.mat");
        mat.color = color;
        mat.SetFloat("_Glossiness", smoothness);
        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material LoadOrCreateMaterial(string path)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Standard"));
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static string FullPath(Transform t) =>
        t.parent == null ? t.name : $"{t.parent.name}/{t.name}";

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
