using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Items;
using Twisted3v3.Stats;

/// <summary>
/// Génère le jeu d'items de base (starters, bottes, légendaires, mythiques) sous forme
/// d'assets <see cref="ItemData"/> + un <see cref="ItemCatalog"/> dans Resources (pour
/// l'auto-chargement du shop). Idempotent : réutilise/écrase les assets existants.
/// Lancer via Tools ▸ Twisted3v3 ▸ Build Items.
/// </summary>
public static class Twisted3v3ItemBuilder
{
    private const string ItemDir = "Assets/_Game/ScriptableObjects/Items";
    private const string ResourcesDir = "Assets/_Game/Resources";

    [MenuItem("Tools/Twisted3v3/Build Items")]
    public static void BuildItems()
    {
        EnsureFolder(ItemDir);
        EnsureFolder(ResourcesDir);

        var all = new List<ItemData>();

        // ------------------------------------------------------------- Starters
        all.Add(Item("IT_LameRouillee", "Lame Rouillée", ItemTier.Starter, 300,
            "Une épée ébréchée mais fiable.",
            Stat(StatType.AttackDamage, 12)));
        all.Add(Item("IT_GrimoireInitie", "Grimoire de l'Initié", ItemTier.Starter, 350,
            "Les premiers secrets de la magie.",
            Stat(StatType.AbilityPower, 20)));
        all.Add(Item("IT_PlastronUse", "Plastron Usé", ItemTier.Starter, 300,
            "Bosselé, mais il tient encore.",
            Stat(StatType.MaxHealth, 80)));

        // --------------------------------------------------------------- Bottes
        all.Add(Item("IT_BottesCeleres", "Bottes Célères", ItemTier.Boots, 300,
            "Vitesse de déplacement accrue.",
            Stat(StatType.MoveSpeed, 1.2f)));
        all.Add(Item("IT_BottesDeGuerre", "Bottes de Guerre", ItemTier.Boots, 900,
            "Mobilité et cadence d'attaque.",
            Stat(StatType.MoveSpeed, 1.0f), Stat(StatType.AttackSpeed, 0.15f)));

        // ----------------------------------------------------------- Légendaires
        all.Add(Item("IT_LameInfini", "Lame d'Infini", ItemTier.Legendary, 3000,
            "Coups critiques dévastateurs.",
            Stat(StatType.AttackDamage, 55), Stat(StatType.CritChance, 0.20f)));
        all.Add(Item("IT_BatonDuVide", "Bâton du Vide", ItemTier.Legendary, 2800,
            "Puissance magique brute.",
            Stat(StatType.AbilityPower, 90)));
        all.Add(Item("IT_ArmureDuColosse", "Armure du Colosse", ItemTier.Legendary, 2700,
            "Endurance physique du titan.",
            Stat(StatType.Armor, 45), Stat(StatType.MaxHealth, 250)));
        all.Add(Item("IT_VoileNegateur", "Voile Négateur", ItemTier.Legendary, 2600,
            "Rempart contre la magie.",
            Stat(StatType.MagicResist, 50), Stat(StatType.MaxHealth, 200)));
        all.Add(Item("IT_FauxSpectrale", "Faux Spectrale", ItemTier.Legendary, 2900,
            "Draine la vie à chaque frappe.",
            Stat(StatType.AttackDamage, 45), Stat(StatType.Lifesteal, 0.15f)));
        all.Add(Item("IT_SceptreArcanique", "Sceptre Arcanique", ItemTier.Legendary, 2900,
            "Sorts amplifiés et raccourcis.",
            Stat(StatType.AbilityPower, 70), Stat(StatType.CooldownReduction, 0.10f)));

        // -------------------------------------------------------------- Mythiques
        all.Add(Item("IT_CouronneDuVide", "Couronne du Vide", ItemTier.Mythic, 3400,
            "MYTHIQUE — apogée de la magie.",
            Stat(StatType.AbilityPower, 100), Stat(StatType.CooldownReduction, 0.20f),
            Stat(StatType.MaxMana, 300)));
        all.Add(Item("IT_EgideEternelle", "Égide Éternelle", ItemTier.Mythic, 3300,
            "MYTHIQUE — forteresse inébranlable.",
            Stat(StatType.MaxHealth, 350), Stat(StatType.Armor, 40),
            Stat(StatType.MagicResist, 40), Stat(StatType.CooldownReduction, 0.10f)));
        all.Add(Item("IT_TrancheAmes", "Tranche-Âmes", ItemTier.Mythic, 3400,
            "MYTHIQUE — le bruiser ultime.",
            Stat(StatType.AttackDamage, 70), Stat(StatType.Lifesteal, 0.15f),
            Stat(StatType.MaxHealth, 300), Stat(StatType.CooldownReduction, 0.10f)));

        // Catalogue dans Resources → chargé par ShopUI si aucun n'est assigné.
        var catalog = LoadOrCreate<ItemCatalog>($"{ResourcesDir}/ItemCatalog.asset");
        catalog.Items = all;
        EditorUtility.SetDirty(catalog);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Twisted3v3] {all.Count} items générés + ItemCatalog (Resources).");
    }

    /// <summary>
    /// Ajoute (si absent) un composant <c>ShopUI</c> à la scène active, sur un objet
    /// « Shop » dédié, et lui assigne le catalogue. À lancer après Build Items, la
    /// scène Map_3v3 ouverte.
    /// </summary>
    [MenuItem("Tools/Twisted3v3/Wire Shop Into Active Scene")]
    public static void WireShopIntoScene()
    {
        var existing = Object.FindFirstObjectByType<Twisted3v3.UI.ShopUI>();
        if (existing != null)
        {
            Debug.Log("[Twisted3v3] ShopUI déjà présent dans la scène.");
            return;
        }

        var go = new GameObject("Shop");
        var shop = go.AddComponent<Twisted3v3.UI.ShopUI>();

        var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>($"{ResourcesDir}/ItemCatalog.asset");
        if (catalog != null)
        {
            var so = new SerializedObject(shop);
            so.FindProperty("_catalog").objectReferenceValue = catalog;
            so.ApplyModifiedProperties();
        }

        EditorUtility.SetDirty(go);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[Twisted3v3] ShopUI ajouté à la scène (objet « Shop »). Sauvegardez la scène.");
    }

    // ------------------------------------------------------------------ Helpers
    private static ItemData Item(string assetName, string display, ItemTier tier, int cost,
                                 string desc, params ItemStat[] stats)
    {
        var item = LoadOrCreate<ItemData>($"{ItemDir}/{assetName}.asset");
        item.DisplayName = display;
        item.Description = desc;
        item.Tier = tier;
        item.TotalCost = cost;
        item.Stats = new List<ItemStat>(stats);
        EditorUtility.SetDirty(item);
        return item;
    }

    private static ItemStat Stat(StatType type, float value)
    {
        // MoveSpeed, AttackSpeed, PV, AD, AP, résistances, mana → bonus plats.
        // CDR / CritChance / Lifesteal sont des fractions 0..1, également en plat.
        return new ItemStat { Stat = type, Value = value, Modifier = ModifierType.Flat };
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        var so = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(so, path);
        return so;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
