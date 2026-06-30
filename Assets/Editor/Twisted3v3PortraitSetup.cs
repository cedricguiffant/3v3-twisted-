using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Twisted3v3.Champions;
using Twisted3v3.UI;

/// <summary>
/// Outil éditeur : importe le portrait PNG de Kaelthar en Sprite et l'assigne au
/// ChampionData et au splash du menu principal (références résolues via AssetDatabase).
/// </summary>
public static class Twisted3v3PortraitSetup
{
    private const string PngPath = "Assets/_Game/Art/Portraits/Kaelthar.png";
    private const string ChampPath = "Assets/_Game/ScriptableObjects/Champions/CH_Kaelthar.asset";
    private const string MenuScene = "Assets/_Game/Scenes/01_MainMenu.unity";

    /// <summary>Garantit que le PNG est importé en Sprite (Single) et renvoie le sprite.</summary>
    private static Sprite EnsureSprite()
    {
        var importer = AssetImporter.GetAtPath(PngPath) as TextureImporter;
        if (importer == null) return null;

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        { importer.textureType = TextureImporterType.Sprite; changed = true; }
        if (importer.spriteImportMode != SpriteImportMode.Single)
        { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
        if (importer.alphaIsTransparency != true)
        { importer.alphaIsTransparency = true; changed = true; }
        if (changed) importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(PngPath);
    }

    /// <summary>Assigne le portrait au ChampionData. Renvoie true si réussi.</summary>
    public static bool WireChampionPortrait()
    {
        var sprite = EnsureSprite();
        if (sprite == null) return false;

        var champ = AssetDatabase.LoadAssetAtPath<ChampionData>(ChampPath);
        if (champ == null) return false;

        champ.Portrait = sprite;
        EditorUtility.SetDirty(champ);
        AssetDatabase.SaveAssets();
        Debug.Log("[Twisted3v3] Portrait assigné à CH_Kaelthar.");
        return true;
    }

    [MenuItem("Tools/Twisted3v3/Wire Kaelthar Portrait (+ menu splash)")]
    public static void WireAll()
    {
        var sprite = EnsureSprite();
        if (sprite == null)
        {
            Debug.LogError("[Twisted3v3] Sprite introuvable : " + PngPath);
            return;
        }

        WireChampionPortrait();

        var scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
        var menu = Object.FindAnyObjectByType<MainMenuController>();
        if (menu != null)
        {
            var so = new SerializedObject(menu);
            so.FindProperty("_splashPortrait").objectReferenceValue = sprite;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(menu);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Twisted3v3] Splash du portrait assigné au menu principal.");
        }
        else Debug.LogWarning("[Twisted3v3] MainMenuController introuvable dans 01_MainMenu.");

        AssetDatabase.SaveAssets();
    }
}

/// <summary>Auto-assigne le portrait au champion une seule fois (sans toucher aux scènes).</summary>
[InitializeOnLoad]
public static class Twisted3v3PortraitAutoSetup
{
    private const string PrefKey = "Twisted3v3.KaeltharPortraitWired";

    static Twisted3v3PortraitAutoSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(PrefKey, false)) return;
            if (Twisted3v3PortraitSetup.WireChampionPortrait())
                EditorPrefs.SetBool(PrefKey, true);
        };
    }
}
