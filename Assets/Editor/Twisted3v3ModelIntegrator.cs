using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Twisted3v3.Combat;

/// <summary>
/// Intégration des modèles 3D Meshy et des textures de map (menu Tools ▸ Twisted3v3 ▸ Art) :
///  - « Integrate All Champion Models » : pour chaque champion (sauf Ragnor, déjà fait),
///    instancie son FBX comme enfant « Model » du prefab PF_*, le redresse (heuristique
///    d'orientation), le met à l'échelle du rôle, pose les pieds au sol, construit un
///    matériau PBR (albedo + normal) et masque la capsule. Les instances de scène héritent.
///  - « Apply Map Textures » : branche Sol/Mur/Tourelle/Nexus sur les matériaux de map
///    et crée des variantes teintées par équipe pour tours et Nexus.
/// Idempotent : ré-exécutable sans dérive.
/// </summary>
public static class Twisted3v3ModelIntegrator
{
    private const string ModelsRoot = "Assets/_Game/Art/Models";
    private const string MatRoot = "Assets/_Game/Art/Materials";
    private const string MapTexRoot = "Assets/_Game/Art/Textures/Map";
    private const string PrefabRoot = "Assets/_Game/Prefabs/Champions";

    // Hauteur cible par rôle (Ragnor 2.6 déjà intégré sur l'instance de scène).
    private static readonly (string Name, float Height)[] Champions =
    {
        ("Kaelthar", 2.5f), ("Lirael", 2.15f), ("Sylvara", 2.2f),
        ("Tharok", 2.75f), ("Vexor", 2.1f),
    };

    // ==================================================== 1. MODÈLES CHAMPIONS
    [MenuItem("Tools/Twisted3v3/Art/Integrate All Champion Models")]
    public static void IntegrateAllChampionModels()
    {
        foreach (var (name, height) in Champions)
            IntegrateChampion(name, height);
        AssetDatabase.SaveAssets();
        Debug.Log("[Models] Intégration des 5 modèles champions terminée.");
    }

    private static void IntegrateChampion(string name, float height)
    {
        string fbxPath = $"{ModelsRoot}/{name}/{name}.fbx";
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbx == null) { Debug.LogWarning($"[Models] {fbxPath} introuvable — {name} sauté."); return; }

        var material = BuildChampionMaterial(name);
        string prefabPath = $"{PrefabRoot}/PF_{name}.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            // Idempotence : retire un modèle déjà intégré.
            var existing = root.transform.Find("Model");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localScale = Vector3.one;

            // Convention Meshy : FBX en Z-up → X-90 les remet debout. (L'heuristique
            // « orientation la plus haute » se faisait piéger par les armes tenues
            // à l'horizontale — Kaelthar/Vexor finissaient couchés.)
            var bestRotation = Quaternion.Euler(-90f, 0f, 0f);
            model.transform.localRotation = bestRotation;

            // Échelle du rôle + pieds au bas de la capsule.
            var bounds = ComputeBounds(model);
            if (bounds.size.y > 0.0001f)
            {
                model.transform.localScale = Vector3.one * (height / bounds.size.y);
                bounds = ComputeBounds(model);
                float feetTarget = root.transform.position.y - root.transform.lossyScale.y;
                model.transform.position += Vector3.up * (feetTarget - bounds.min.y);
            }

            // Matériau PBR + capsule masquée (ChampionVisuals capturera cet état).
            foreach (var rend in model.GetComponentsInChildren<Renderer>())
            {
                var mats = new Material[Mathf.Max(1, rend.sharedMaterials.Length)];
                for (int i = 0; i < mats.Length; i++) mats[i] = material;
                rend.sharedMaterials = mats;
            }
            if (root.TryGetComponent<MeshRenderer>(out var capsule)) capsule.enabled = false;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"[Models] {name} : modèle intégré (rot {bestRotation.eulerAngles}, h {height}, échelle {model.transform.localScale.x:0.###}).");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    /// <summary>Matériau Standard : albedo + normal map (marquée NormalMap à l'import).</summary>
    private static Material BuildChampionMaterial(string name)
    {
        string normalPath = $"{ModelsRoot}/{name}/{name}_Normal.png";
        var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
        {
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
        }

        var mat = LoadOrCreateMaterial($"{MatRoot}/M_{name}Model.mat");
        mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ModelsRoot}/{name}/{name}_Albedo.png");
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }
        mat.color = Color.white;
        mat.SetFloat("_Glossiness", 0.35f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ======================================================= 2. TEXTURES DE MAP
    [MenuItem("Tools/Twisted3v3/Art/Apply Map Textures")]
    public static void ApplyMapTextures()
    {
        var sol = AssetDatabase.LoadAssetAtPath<Texture2D>($"{MapTexRoot}/Sol.jpg");
        var herbe = AssetDatabase.LoadAssetAtPath<Texture2D>($"{MapTexRoot}/Herbe.jpg");
        var mur = AssetDatabase.LoadAssetAtPath<Texture2D>($"{MapTexRoot}/Mur.jpg");
        var tourelle = AssetDatabase.LoadAssetAtPath<Texture2D>($"{MapTexRoot}/Tourelle.jpg");
        var nexus = AssetDatabase.LoadAssetAtPath<Texture2D>($"{MapTexRoot}/Nexus.jpg");

        // Sol volcanique ; jungle en HERBE (texture dédiée) ; lanes teintées sable.
        Texture(sol, $"{MatRoot}/Map/M_Map_Ground.mat", new Color(0.90f, 0.92f, 0.86f), new Vector2(6f, 6f));
        Texture(herbe, $"{MatRoot}/Map/M_Map_Jungle.mat", new Color(0.92f, 1f, 0.9f), new Vector2(5f, 7f));
        Texture(sol, $"{MatRoot}/Map/M_Map_Lane.mat", new Color(1.00f, 0.92f, 0.72f), new Vector2(1.5f, 10f));
        Texture(mur, $"{MatRoot}/Map/M_Map_Wall.mat", Color.white, new Vector2(2f, 1f));

        // Tours et Nexus : texture dédiée, teintée par équipe.
        var towerBlue = Texture(tourelle, $"{MatRoot}/Map/M_Map_TowerBlue.mat", new Color(0.70f, 0.82f, 1f), Vector2.one);
        var towerRed = Texture(tourelle, $"{MatRoot}/Map/M_Map_TowerRed.mat", new Color(1f, 0.70f, 0.64f), Vector2.one);
        var nexusBlue = Texture(nexus, $"{MatRoot}/Map/M_Map_NexusBlue.mat", new Color(0.70f, 0.82f, 1f), Vector2.one);
        var nexusRed = Texture(nexus, $"{MatRoot}/Map/M_Map_NexusRed.mat", new Color(1f, 0.70f, 0.64f), Vector2.one);

        int assigned = 0;
        foreach (var s in Object.FindObjectsByType<Structure>(FindObjectsSortMode.None))
        {
            if (!s.TryGetComponent<MeshRenderer>(out var rend)) continue;
            bool isTower = s.GetComponent<TowerWeapon>() != null;
            bool isBlue = s.Team == Twisted3v3.Core.Team.Blue;
            rend.sharedMaterial = isTower ? (isBlue ? towerBlue : towerRed)
                                          : (isBlue ? nexusBlue : nexusRed);
            assigned++;
            Debug.Log($"[MapTex] {s.name} → {rend.sharedMaterial.name}.");
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[MapTex] Textures de map appliquées (sol/jungle/lanes/murs + {assigned} bâtiments), scène sauvée.");
    }

    private static Material Texture(Texture2D tex, string path, Color tint, Vector2 tiling)
    {
        var mat = LoadOrCreateMaterial(path);
        if (tex != null)
        {
            mat.mainTexture = tex;
            mat.mainTextureScale = tiling;
        }
        mat.color = tint;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ================================================================== HELPERS
    private static Bounds ComputeBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    private static Material LoadOrCreateMaterial(string path)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Standard"));
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
