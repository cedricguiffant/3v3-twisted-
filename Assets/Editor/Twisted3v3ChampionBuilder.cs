using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Stats;
using Twisted3v3.Abilities;

/// <summary>
/// Génère les assets (ScriptableObjects de capacités + ChampionData) et les prefabs
/// jouables (stack IA) des champions restants : Sylvara, Vexor, Tharok.
/// Lancer via le menu Tools ▸ Twisted3v3 ▸ Build Remaining Champions.
/// </summary>
public static class Twisted3v3ChampionBuilder
{
    private const string AbRoot = "Assets/_Game/ScriptableObjects/Abilities";
    private const string ChampRoot = "Assets/_Game/ScriptableObjects/Champions";
    private const string PrefabRoot = "Assets/_Game/Prefabs/Champions";

    [MenuItem("Tools/Twisted3v3/Build Remaining Champions (Sylvara, Vexor, Tharok)")]
    public static void BuildAll()
    {
        EnsureFolders();
        BuildSylvara();
        BuildVexor();
        BuildTharok();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Twisted3v3] Sylvara, Vexor et Tharok générés (assets + prefabs).");
    }

    // ----------------------------------------------------------------- CHAMPIONS
    private static void BuildSylvara()
    {
        string dir = AbDir("Sylvara");
        var p = Ability<Twisted3v3.Champions.Sylvara.Sylvara_Passive_EpinesVivantes>(dir, "AB_Sylvara_Passive", AbilitySlot.Passive, "Épines Vivantes", 5);
        var q = Ability<Twisted3v3.Champions.Sylvara.Sylvara_Q_VrilleDEpines>(dir, "AB_Sylvara_Q", AbilitySlot.Q, "Vrille d'Épines", 5);
        var z = Ability<Twisted3v3.Champions.Sylvara.Sylvara_Z_MurDeRonces>(dir, "AB_Sylvara_Z", AbilitySlot.Z, "Mur de Ronces", 5);
        var e = Ability<Twisted3v3.Champions.Sylvara.Sylvara_E_TransfertSylvestre>(dir, "AB_Sylvara_E", AbilitySlot.E, "Transfert Sylvestre", 5);
        var r = Ability<Twisted3v3.Champions.Sylvara.Sylvara_R_ForetPrimordiale>(dir, "AB_Sylvara_R", AbilitySlot.R, "Forêt Primordiale", 3);

        var stats = new List<BaseStatEntry>();
        Add(stats, StatType.MaxHealth, 900, 70); Add(stats, StatType.HealthRegen, 5, 0.5f);
        Add(stats, StatType.Armor, 24, 3); Add(stats, StatType.MagicResist, 30, 1.2f);
        Add(stats, StatType.MaxMana, 380, 50); Add(stats, StatType.ManaRegen, 9, 0.7f);
        Add(stats, StatType.AttackDamage, 52, 2.8f); Add(stats, StatType.AbilityPower, 60, 0);
        Add(stats, StatType.AttackSpeed, 0.62f, 0); Add(stats, StatType.MoveSpeed, 6, 0);
        Add(stats, StatType.AttackRange, 5, 0);

        var champ = Champion("CH_Sylvara", "Sylvara", ChampionRole.Mage, p, q, z, e, r, stats);
        BuildPrefab("PF_Sylvara", champ, new Color(0.35f, 0.75f, 0.45f), ranged: true);
    }

    private static void BuildVexor()
    {
        string dir = AbDir("Vexor");
        var p = Ability<Twisted3v3.Champions.Vexor.Vexor_Passive_OmbreFuyante>(dir, "AB_Vexor_Passive", AbilitySlot.Passive, "Ombre Fuyante", 5);
        var q = Ability<Twisted3v3.Champions.Vexor.Vexor_Q_LameFantome>(dir, "AB_Vexor_Q", AbilitySlot.Q, "Lame Fantôme", 5);
        var z = Ability<Twisted3v3.Champions.Vexor.Vexor_Z_BondSpectral>(dir, "AB_Vexor_Z", AbilitySlot.Z, "Bond Spectral", 5);
        var e = Ability<Twisted3v3.Champions.Vexor.Vexor_E_FrappeDuNeant>(dir, "AB_Vexor_E", AbilitySlot.E, "Frappe du Néant", 5);
        var r = Ability<Twisted3v3.Champions.Vexor.Vexor_R_ExecutionDesOmbres>(dir, "AB_Vexor_R", AbilitySlot.R, "Exécution des Ombres", 3);

        var stats = new List<BaseStatEntry>();
        Add(stats, StatType.MaxHealth, 920, 72); Add(stats, StatType.HealthRegen, 6, 0.5f);
        Add(stats, StatType.Armor, 28, 3.2f); Add(stats, StatType.MagicResist, 30, 1.2f);
        Add(stats, StatType.MaxMana, 300, 35); Add(stats, StatType.ManaRegen, 8, 0.6f);
        Add(stats, StatType.AttackDamage, 68, 3.5f); Add(stats, StatType.AttackSpeed, 0.68f, 0.02f);
        Add(stats, StatType.MoveSpeed, 6.2f, 0); Add(stats, StatType.AttackRange, 2, 0);

        var champ = Champion("CH_Vexor", "Vexor", ChampionRole.Assassin, p, q, z, e, r, stats);
        BuildPrefab("PF_Vexor", champ, new Color(0.55f, 0.3f, 0.7f), ranged: false);
    }

    private static void BuildTharok()
    {
        string dir = AbDir("Tharok");
        var p = Ability<Twisted3v3.Champions.Tharok.Tharok_Passive_PeauDePierre>(dir, "AB_Tharok_Passive", AbilitySlot.Passive, "Peau de Pierre", 5);
        var q = Ability<Twisted3v3.Champions.Tharok.Tharok_Q_CoupDeMassue>(dir, "AB_Tharok_Q", AbilitySlot.Q, "Coup de Massue", 5);
        var z = Ability<Twisted3v3.Champions.Tharok.Tharok_Z_RempartVivant>(dir, "AB_Tharok_Z", AbilitySlot.Z, "Rempart Vivant", 5);
        var e = Ability<Twisted3v3.Champions.Tharok.Tharok_E_SautSismique>(dir, "AB_Tharok_E", AbilitySlot.E, "Saut Sismique", 5);
        var r = Ability<Twisted3v3.Champions.Tharok.Tharok_R_ForteresseImperiale>(dir, "AB_Tharok_R", AbilitySlot.R, "Forteresse Impériale", 3);

        var stats = new List<BaseStatEntry>();
        Add(stats, StatType.MaxHealth, 1250, 95); Add(stats, StatType.HealthRegen, 8, 0.7f);
        Add(stats, StatType.Armor, 40, 4.5f); Add(stats, StatType.MagicResist, 35, 1.6f);
        Add(stats, StatType.MaxMana, 320, 40); Add(stats, StatType.ManaRegen, 8, 0.6f);
        Add(stats, StatType.AttackDamage, 60, 3); Add(stats, StatType.AttackSpeed, 0.6f, 0);
        Add(stats, StatType.MoveSpeed, 6, 0); Add(stats, StatType.AttackRange, 2, 0);

        var champ = Champion("CH_Tharok", "Tharok", ChampionRole.Tank, p, q, z, e, r, stats);
        BuildPrefab("PF_Tharok", champ, new Color(0.5f, 0.55f, 0.62f), ranged: false);
    }

    // ----------------------------------------------------------------- HELPERS
    private static T Ability<T>(string dir, string file, AbilitySlot slot, string display, int maxRank) where T : AbilityData
    {
        var a = ScriptableObject.CreateInstance<T>();
        a.DisplayName = display;
        a.Slot = slot;
        a.MaxRank = maxRank;
        AssetDatabase.CreateAsset(a, dir + "/" + file + ".asset");
        EditorUtility.SetDirty(a);
        return a;
    }

    private static ChampionData Champion(string file, string display, ChampionRole role,
        AbilityData p, AbilityData q, AbilityData z, AbilityData e, AbilityData r, List<BaseStatEntry> stats)
    {
        var c = ScriptableObject.CreateInstance<ChampionData>();
        c.DisplayName = display; c.Role = role;
        c.Passive = p; c.Q = q; c.Z = z; c.E = e; c.R = r;
        c.BaseStats = stats;
        AssetDatabase.CreateAsset(c, ChampRoot + "/" + file + ".asset");
        EditorUtility.SetDirty(c);
        return c;
    }

    private static void BuildPrefab(string name, ChampionData data, Color color, bool ranged)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name; go.layer = 10; // Units

        // Matériau dédié (asset) — ne pas modifier le matériau partagé par défaut.
        if (!AssetDatabase.IsValidFolder("Assets/_Game/Art/Materials"))
            AssetDatabase.CreateFolder("Assets/_Game/Art", "Materials");
        string matPath = "Assets/_Game/Art/Materials/M_" + name + ".mat";
        var mat = new Material(Shader.Find("Standard")) { color = color };
        AssetDatabase.CreateAsset(mat, matPath);
        go.GetComponent<Renderer>().sharedMaterial = mat;

        var champ = go.AddComponent<Champion>();                  // ajoute AbilitySystem (RequireComponent)
        var agent = go.AddComponent<NavMeshAgent>(); agent.radius = 0.5f; agent.height = 2f; agent.stoppingDistance = 0.1f;
        var aa = go.AddComponent<Twisted3v3.Combat.AutoAttack>();
        go.AddComponent<Twisted3v3.Progression.LevelSystem>();
        go.AddComponent<Twisted3v3.Economy.GoldWallet>();
        var bounty = go.AddComponent<Twisted3v3.Combat.KillReward>(); bounty.Gold = 300; bounty.Experience = 220f;
        go.AddComponent<Twisted3v3.Combat.RespawnController>();
        go.AddComponent<Twisted3v3.AI.ChampionAI>();
        var bar = go.AddComponent<Twisted3v3.UI.WorldHealthBar>(); bar.SetColor(color);

        var soC = new SerializedObject(champ);
        soC.FindProperty("_data").objectReferenceValue = data;
        soC.FindProperty("_team").enumValueIndex = 1; // Blue par défaut (à ré-affecter au placement)
        soC.FindProperty("_level").intValue = 6;
        soC.ApplyModifiedProperties();

        var soA = new SerializedObject(aa);
        soA.FindProperty("_ranged").boolValue = ranged;
        soA.FindProperty("_unitMask").intValue = (1 << 10);
        soA.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(go, PrefabRoot + "/" + name + ".prefab");
        Object.DestroyImmediate(go);
    }

    private static void Add(List<BaseStatEntry> list, StatType stat, float baseValue, float perLevel)
    {
        var en = new BaseStatEntry { Stat = stat, BaseValue = baseValue, PerLevel = perLevel };
        list.Add(en);
    }

    private static string AbDir(string champ)
    {
        string dir = AbRoot + "/" + champ;
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(AbRoot, champ);
        return dir;
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Game/ScriptableObjects")) AssetDatabase.CreateFolder("Assets/_Game", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(AbRoot)) AssetDatabase.CreateFolder("Assets/_Game/ScriptableObjects", "Abilities");
        if (!AssetDatabase.IsValidFolder(ChampRoot)) AssetDatabase.CreateFolder("Assets/_Game/ScriptableObjects", "Champions");
        if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs")) AssetDatabase.CreateFolder("Assets/_Game", "Prefabs");
        if (!AssetDatabase.IsValidFolder(PrefabRoot)) AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "Champions");
    }
}
