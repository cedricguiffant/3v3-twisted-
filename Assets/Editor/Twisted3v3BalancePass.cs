using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Twisted3v3.Champions;
using Twisted3v3.Champions.Ragnor;
using Twisted3v3.Combat;
using Twisted3v3.Match;
using Twisted3v3.Stats;

/// <summary>
/// Pass d'équilibrage « départ niveau 1 / partie 10 min » (menu Tools ▸ Twisted3v3) :
///  - tous les champions (prefabs + instances de scène) démarrent NIVEAU 1 ;
///  - auto-attaques sur l'AttackDamage réel (fin du 100 fixe de debug) ;
///  - Ragnor freiné en début de partie (AD de base -10, dégâts rang 1 réduits,
///    croissance relevée pour garder le même late game) ;
///  - MatchManager : limite à 100 kills, durée max 10 minutes ;
///  - tours 1200 PV / Nexus 2500 PV (les parties se concluent plus vite).
/// Idempotent : les valeurs sont écrites en absolu, ré-exécutable sans dérive.
/// </summary>
public static class Twisted3v3BalancePass
{
    private static readonly string[] ChampionPrefabs =
    {
        "PF_Kaelthar", "PF_Ragnor", "PF_Lirael", "PF_Sylvara", "PF_Vexor", "PF_Tharok"
    };

    [MenuItem("Tools/Twisted3v3/Balance Pass (Niveau 1, 10 min, 100 kills)")]
    public static void Apply()
    {
        int touched = 0;

        // ---------------------------------------------- 1. Prefabs champions
        foreach (var name in ChampionPrefabs)
        {
            string path = $"Assets/_Game/Prefabs/Champions/{name}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (NormalizeChampion(root)) { PrefabUtility.SaveAsPrefabAsset(root, path); touched++; }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        Debug.Log($"[Balance] {touched} prefab(s) champion normalisés (niveau 1, AD réel).");

        // ------------------------------------------- 2. Instances de la scène
        int sceneChamps = 0;
        foreach (var champ in Object.FindObjectsByType<Champion>(FindObjectsSortMode.None))
            if (NormalizeChampion(champ.gameObject)) sceneChamps++;
        Debug.Log($"[Balance] {sceneChamps} champion(s) de scène normalisés.");

        // ----------------------------------------------------- 3. MatchManager
        var match = Object.FindFirstObjectByType<MatchManager>();
        if (match != null)
        {
            var so = new SerializedObject(match);
            so.FindProperty("_killsToWin").intValue = 100;
            so.FindProperty("_matchDuration").floatValue = 600f;
            so.ApplyModifiedProperties();
            Debug.Log("[Balance] MatchManager : 100 kills max, durée 600 s (10 min).");
        }

        // ------------------------------------------------------- 4. Structures
        foreach (var s in Object.FindObjectsByType<Structure>(FindObjectsSortMode.None))
        {
            bool isTower = s.GetComponent<TowerWeapon>() != null;
            float hp = isTower ? 1200f : 2500f;
            var so = new SerializedObject(s);
            so.FindProperty("_maxHealth").floatValue = hp;
            so.ApplyModifiedProperties();
            Debug.Log($"[Balance] {s.name} : {hp} PV.");
        }

        // ------------------------------------------------- 5. Nerf early Ragnor
        NerfRagnor();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Balance] Pass terminé — scène et assets sauvegardés.");
    }

    /// <summary>Niveau 1 + auto-attaque sur l'AD réel. Renvoie vrai si modifié.</summary>
    private static bool NormalizeChampion(GameObject go)
    {
        bool changed = false;

        var champ = go.GetComponent<Champion>();
        if (champ != null)
        {
            var so = new SerializedObject(champ);
            var level = so.FindProperty("_level");
            if (level != null && level.intValue != 1)
            {
                level.intValue = 1;
                so.ApplyModifiedProperties();
                changed = true;
            }
        }

        var attack = go.GetComponent<AutoAttack>();
        if (attack != null)
        {
            var so = new SerializedObject(attack);
            var fixedDmg = so.FindProperty("_useFixedDamage");
            if (fixedDmg != null && fixedDmg.boolValue)
            {
                fixedDmg.boolValue = false;
                so.ApplyModifiedProperties();
                changed = true;
            }
        }
        return changed;
    }

    /// <summary>
    /// Ragnor dominait le début de partie : AD de base 72 → 62 (croissance 4 → 4.6,
    /// même total au niveau 18) et dégâts de sorts réduits aux premiers rangs
    /// (rangs hauts conservés — le late game ne change pas).
    /// </summary>
    private static void NerfRagnor()
    {
        var data = AssetDatabase.LoadAssetAtPath<ChampionData>(
            "Assets/_Game/ScriptableObjects/Champions/CH_Ragnor.asset");
        if (data != null)
        {
            for (int i = 0; i < data.BaseStats.Count; i++)
            {
                var entry = data.BaseStats[i];
                if (entry.Stat != StatType.AttackDamage) continue;
                entry.BaseValue = 62f;
                entry.PerLevel = 4.6f;
                data.BaseStats[i] = entry;
            }
            EditorUtility.SetDirty(data);
            Debug.Log("[Balance] CH_Ragnor : AD 62 (+4.6/niv) — early -14%, niveau 18 inchangé.");
        }

        const string dir = "Assets/_Game/ScriptableObjects/Abilities/Ragnor";

        var q = AssetDatabase.LoadAssetAtPath<Ragnor_Q_CoupDeMarteau>($"{dir}/AB_Ragnor_Q.asset");
        if (q != null)
        {
            q.BaseDamageByRank = new[] { 42f, 85f, 128f, 165f, 200f };
            EditorUtility.SetDirty(q);
        }

        var z = AssetDatabase.LoadAssetAtPath<Ragnor_Z_TremblementVolcanique>($"{dir}/AB_Ragnor_Z.asset");
        if (z != null)
        {
            z.BaseDamageByRank = new[] { 50f, 100f, 150f, 190f, 230f };
            EditorUtility.SetDirty(z);
        }

        var e = AssetDatabase.LoadAssetAtPath<Ragnor_E_ChargeEnflammee>($"{dir}/AB_Ragnor_E.asset");
        if (e != null)
        {
            e.BaseDamageByRank = new[] { 35f, 70f, 110f, 140f, 170f };
            EditorUtility.SetDirty(e);
        }

        var r = AssetDatabase.LoadAssetAtPath<Ragnor_R_ApocalypseDesCendres>($"{dir}/AB_Ragnor_R.asset");
        if (r != null)
        {
            r.ImpactDamageByRank = new[] { 150f, 265f, 380f };
            r.ZoneDamagePerTickByRank = new[] { 20f, 38f, 55f };
            EditorUtility.SetDirty(r);
        }

        Debug.Log("[Balance] Sorts de Ragnor : rangs 1-2 réduits (Q 60→42, Z 70→50, E 50→35, R 180→150).");
    }
}
