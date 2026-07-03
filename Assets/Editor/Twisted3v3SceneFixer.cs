using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Twisted3v3.Champions;
using Twisted3v3.Combat;
using Twisted3v3.Core;
using Twisted3v3.VFX;

/// <summary>
/// Corrections de scène en un clic (menu Tools ▸ Twisted3v3) :
///  - « Fix Spawns & Collisions » :
///      1. Colliders des murs recalés sur leur visuel (murs invisibles) ;
///      2. Tours/Nexus rendus bloquants (NavMeshObstacle carve) ;
///      3. NavMesh REBAKÉ (source n°1 des murs invisibles : bake obsolète) ;
///      4. Champions déplacés dans la fontaine de leur équipe (3 slots espacés),
///         anciens points de respawn neutralisés → respawn = fontaine ;
///      5. Composants dupliqués des prefabs champions dédoublonnés ;
///      puis sauvegarde de la scène. Idempotent.
///  - « Apply Pending Fixes » : Beautify Map (v2 éclaircie) + tout ce qui précède.
/// </summary>
public static class Twisted3v3SceneFixer
{
    private static readonly string[] ChampionPrefabs =
    {
        "PF_Kaelthar", "PF_Ragnor", "PF_Lirael", "PF_Sylvara", "PF_Vexor", "PF_Tharok"
    };

    [MenuItem("Tools/Twisted3v3/Apply Pending Fixes (Map + Spawns + Collisions)")]
    public static void ApplyPendingFixes()
    {
        Twisted3v3ArtPass.BeautifyMap();
        FixSpawnsAndCollisions();
    }

    // ================================================================= ROSTER 3v3
    /// <summary>Composition attendue de Map_3v3 : nom, prefab, équipe, joueur par défaut.</summary>
    private static readonly (string Name, string Prefab, Team Team, bool Player)[] Roster =
    {
        ("Kaelthar", "PF_Kaelthar", Team.Blue, true),
        ("Lirael",   "PF_Lirael",   Team.Blue, false),
        ("Sylvara",  "PF_Sylvara",  Team.Blue, false),
        ("Ragnor",   "PF_Ragnor",   Team.Red,  false),
        ("Vexor",    "PF_Vexor",    Team.Red,  false),
        ("Tharok",   "PF_Tharok",   Team.Red,  false),
    };

    /// <summary>
    /// Reconstruit le 3v3 complet : ré-instancie les champions manquants, répare les
    /// instances corrompues (composants dupliqués, PlayerController perdu, IA sur le
    /// joueur), assigne les équipes, puis replace tout le monde en fontaine. Idempotent.
    /// </summary>
    [MenuItem("Tools/Twisted3v3/Restore Roster (3v3)")]
    public static void RestoreRoster()
    {
        var log = new StringBuilder("[RestoreRoster]\n");
        DedupeAllPrefabComponents(log);

        var champions = new List<Champion>(
            Object.FindObjectsByType<Champion>(FindObjectsSortMode.None));

        foreach (var entry in Roster)
        {
            var champ = FindChampion(champions, entry.Name);
            if (champ == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/_Game/Prefabs/Champions/{entry.Prefab}.prefab");
                if (prefab == null) { log.AppendLine($"  ⚠ prefab {entry.Prefab} introuvable."); continue; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = entry.Name;
                champ = go.GetComponent<Champion>();
                champions.Add(champ);
                log.AppendLine($"  {entry.Name} ré-instancié depuis {entry.Prefab}.");
            }
            NormalizeChampionInstance(champ, entry.Team, entry.Player, log);
        }

        MoveSpawnsToFountains(log);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        log.AppendLine("  Scène sauvegardée.");
        Debug.Log(log.ToString());
    }

    private static Champion FindChampion(List<Champion> all, string name)
    {
        foreach (var c in all)
        {
            if (c == null) continue;
            if (c.Data != null && string.Equals(c.Data.DisplayName, name,
                    System.StringComparison.OrdinalIgnoreCase)) return c;
            if (c.name.ToLowerInvariant().Contains(name.ToLowerInvariant())) return c;
        }
        return null;
    }

    /// <summary>
    /// Répare une instance de champion : doublons retirés (overrides ajoutés), équipe
    /// forcée, contrôle joueur/IA remis d'équerre. Les composants hérités du prefab ne
    /// sont pas destructibles sur l'instance (exception Unity) → try/catch, le
    /// PlayerChampionBinder les neutralise de toute façon au runtime.
    /// </summary>
    private static void NormalizeChampionInstance(Champion champ, Team team, bool player,
        StringBuilder log)
    {
        var go = champ.gameObject;

        // 1) Doublons de composants (plusieurs passes : RequireComponent impose un ordre).
        int removed = 0;
        for (int pass = 0; pass < 4; pass++)
        {
            bool any = false;
            var seen = new Dictionary<System.Type, Component>();
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null || comp is Transform) continue;
                var type = comp.GetType();
                if (!seen.ContainsKey(type)) { seen[type] = comp; continue; }
                try { Object.DestroyImmediate(comp); removed++; any = true; }
                catch (System.Exception) { /* composant du prefab ou requis — ignoré */ }
            }
            if (!any) break;
        }
        if (removed > 0) log.AppendLine($"  {go.name} : {removed} doublon(s) retiré(s).");

        // 2) Équipe.
        var champComp = go.GetComponent<Champion>();
        var so = new SerializedObject(champComp);
        var teamProp = so.FindProperty("_team");
        if (teamProp != null && teamProp.enumValueIndex != (int)team)
        {
            teamProp.enumValueIndex = (int)team;
            so.ApplyModifiedProperties();
            log.AppendLine($"  {go.name} : équipe → {team}.");
        }

        // 3) Contrôle joueur / IA.
        if (player)
        {
            foreach (var ai in go.GetComponents<Twisted3v3.AI.ChampionAI>())
            {
                try { Object.DestroyImmediate(ai); log.AppendLine($"  {go.name} : ChampionAI retiré (joueur)."); }
                catch (System.Exception) { }
            }
            if (go.GetComponent<Twisted3v3.Player.PlayerController>() == null)
            {
                var pc = go.AddComponent<Twisted3v3.Player.PlayerController>();
                var pcSo = new SerializedObject(pc);
                pcSo.FindProperty("_groundMask").intValue = LayerMask.GetMask("Ground");
                pcSo.FindProperty("_unitMask").intValue = LayerMask.GetMask("Units");
                pcSo.ApplyModifiedProperties();
                log.AppendLine($"  {go.name} : PlayerController restauré (joueur par défaut).");
            }
        }
        else
        {
            var pc = go.GetComponent<Twisted3v3.Player.PlayerController>();
            if (pc != null)
                try { Object.DestroyImmediate(pc); log.AppendLine($"  {go.name} : PlayerController retiré (bot)."); }
                catch (System.Exception) { /* hérité du prefab → binder runtime */ }
            var harness = go.GetComponent<Twisted3v3.DebugTools.ChampionTestHarness>();
            if (harness != null)
                try { Object.DestroyImmediate(harness); } catch (System.Exception) { }
            if (go.GetComponent<Twisted3v3.AI.ChampionAI>() == null)
            {
                go.AddComponent<Twisted3v3.AI.ChampionAI>();
                log.AppendLine($"  {go.name} : ChampionAI ajouté.");
            }
        }
    }

    /// <summary>Doublons de composants de TOUT type sur les 6 prefabs champions.</summary>
    private static void DedupeAllPrefabComponents(StringBuilder log)
    {
        foreach (var name in ChampionPrefabs)
        {
            string path = $"Assets/_Game/Prefabs/Champions/{name}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int removed = 0;
                for (int pass = 0; pass < 4; pass++)
                {
                    bool any = false;
                    var seen = new Dictionary<System.Type, Component>();
                    foreach (var comp in root.GetComponents<Component>())
                    {
                        if (comp == null || comp is Transform) continue;
                        var type = comp.GetType();
                        if (!seen.ContainsKey(type)) { seen[type] = comp; continue; }
                        try { Object.DestroyImmediate(comp); removed++; any = true; }
                        catch (System.Exception) { }
                    }
                    if (!any) break;
                }
                if (removed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    log.AppendLine($"  Prefab {name} : {removed} doublon(s) retiré(s).");
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }

    [MenuItem("Tools/Twisted3v3/Fix Spawns & Collisions")]
    public static void FixSpawnsAndCollisions()
    {
        var log = new StringBuilder("[SceneFixer]\n");

        FixWallColliders(log);
        AddStructureObstacles(log);
        RebakeNavMesh(log);
        MoveSpawnsToFountains(log);
        DedupePrefabComponents(log);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        log.AppendLine("  Scène sauvegardée.");
        Debug.Log(log.ToString());
    }

    // ------------------------------------------------ 1. Colliders des murs
    /// <summary>Recale le BoxCollider de chaque mur sur les bounds exactes de son mesh.</summary>
    private static void FixWallColliders(StringBuilder log)
    {
        int fixedCount = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            string n = t.name.ToLowerInvariant();
            string pn = t.parent != null ? t.parent.name.ToLowerInvariant() : "";
            if (!n.Contains("wall") && !n.Contains("mur") && !pn.Contains("wall")) continue;
            if (!t.TryGetComponent<MeshFilter>(out var mf) || mf.sharedMesh == null) continue;

            var bc = t.GetComponent<BoxCollider>();
            if (bc == null) bc = t.gameObject.AddComponent<BoxCollider>();
            bc.center = mf.sharedMesh.bounds.center;
            bc.size = mf.sharedMesh.bounds.size;
            fixedCount++;
        }
        log.AppendLine($"  Murs : {fixedCount} collider(s) recalé(s) sur le visuel.");
    }

    // ------------------------------------------- 2. Structures bloquantes
    /// <summary>Tours et Nexus creusent le NavMesh (on ne traverse plus les bâtiments).</summary>
    private static void AddStructureObstacles(StringBuilder log)
    {
        int added = 0;
        foreach (var s in Object.FindObjectsByType<Structure>(FindObjectsSortMode.None))
        {
            if (s.GetComponent<NavMeshObstacle>() != null) continue;
            var rend = s.GetComponentInChildren<Renderer>();
            if (rend == null) continue;

            var obstacle = s.gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            var ls = s.transform.lossyScale;
            obstacle.size = new Vector3(
                rend.bounds.size.x / Mathf.Max(0.001f, ls.x),
                rend.bounds.size.y / Mathf.Max(0.001f, ls.y),
                rend.bounds.size.z / Mathf.Max(0.001f, ls.z));
            obstacle.center = s.transform.InverseTransformPoint(rend.bounds.center);
            obstacle.carving = true;
            added++;
        }
        log.AppendLine($"  Structures : {added} NavMeshObstacle (carve) ajouté(s) aux tours/Nexus.");
    }

    // ------------------------------------------------------ 3. Rebake NavMesh
    /// <summary>Rebake complet — élimine les murs invisibles dus à un bake obsolète.</summary>
    private static void RebakeNavMesh(StringBuilder log)
    {
        int baked = 0;
        foreach (var surface in Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None))
        {
            surface.BuildNavMesh();
            baked++;
        }
        int verts = NavMesh.CalculateTriangulation().vertices.Length;
        log.AppendLine($"  NavMesh : {baked} surface(s) rebakée(s) ({verts} sommets).");
    }

    // -------------------------------------------------- 4. Spawns en fontaine
    /// <summary>
    /// Place les champions de chaque équipe dans le rayon de leur fontaine (3 slots
    /// espacés, tournés vers le centre) et neutralise les anciens _spawnPoint pour que
    /// le respawn utilise cette nouvelle position (comportement par défaut du
    /// RespawnController quand le point est vide).
    /// </summary>
    private static void MoveSpawnsToFountains(StringBuilder log)
    {
        var champions = Object.FindObjectsByType<Champion>(FindObjectsSortMode.None);

        foreach (var fountain in Object.FindObjectsByType<FountainZone>(FindObjectsSortMode.None))
        {
            var slots = new System.Collections.Generic.List<Champion>();
            foreach (var c in champions)
                if (c.Team == fountain.Team) slots.Add(c);
            slots.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            if (slots.Count == 0) continue;

            Vector3 center = fountain.transform.position;
            Vector3 toMid = -center; toMid.y = 0f;
            toMid = toMid.sqrMagnitude > 0.01f ? toMid.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, toMid);

            for (int i = 0; i < slots.Count; i++)
            {
                var champ = slots[i];
                float lateral = (i - (slots.Count - 1) * 0.5f) * 3f;
                Vector3 pos = center + toMid * 2.5f + right * lateral;
                // Hauteur : jamais sous celle de la fontaine (y=1) — certains prefabs
                // ont une racine à y=0 et finiraient enfoncés dans le socle de la base.
                pos.y = Mathf.Max(champ.transform.position.y, center.y);
                champ.transform.position = pos;
                champ.transform.rotation = Quaternion.LookRotation(toMid);

                // Respawn = cette position : on vide l'ancien point de spawn.
                if (champ.TryGetComponent<RespawnController>(out var rc))
                {
                    var so = new SerializedObject(rc);
                    var sp = so.FindProperty("_spawnPoint");
                    if (sp.objectReferenceValue != null)
                    {
                        sp.objectReferenceValue = null;
                        so.ApplyModifiedProperties();
                    }
                }
                log.AppendLine($"  Spawn : {champ.name} ({fountain.Team}) → fontaine {Fmt(pos)}.");
            }
        }
    }

    // ------------------------------------------------- 5. Dédoublonnage prefabs
    /// <summary>
    /// Supprime les doublons de composants ajoutés par des ré-émissions MCP (ex:
    /// 7× CombatFeedback sur PF_Kaelthar = 7 textes de dégâts superposés).
    /// </summary>
    private static void DedupePrefabComponents(StringBuilder log)
    {
        var types = new[]
        {
            typeof(CombatFeedback), typeof(AbilityCastVfx), typeof(Twisted3v3.Items.Inventory)
        };

        foreach (var name in ChampionPrefabs)
        {
            string path = $"Assets/_Game/Prefabs/Champions/{name}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int removed = 0;
                foreach (var type in types)
                {
                    var comps = root.GetComponents(type);
                    for (int i = 1; i < comps.Length; i++)
                    {
                        Object.DestroyImmediate(comps[i]);
                        removed++;
                    }
                }
                if (removed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    log.AppendLine($"  Doublons : {removed} composant(s) retiré(s) de {name}.");
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }

    // ============================================================ LANES DE MINIONS
    /// <summary>
    /// Reconstruit les 4 spawners de minions : apparition AU NEXUS de leur équipe,
    /// chemin par les lanes (x=±18, jamais la jungle) jusqu'au Nexus ennemi — la tour
    /// adverse est rencontrée en route (priorité gérée par le Minion). Intervalle de
    /// vague ramené à 12 s (fréquence doublée). Idempotent.
    /// </summary>
    [MenuItem("Tools/Twisted3v3/Rebuild Minion Lanes (x2 fréquence)")]
    public static void RebuildMinionLanes()
    {
        // Nexus de chaque équipe (Structure pilotée par NexusController).
        Vector3? blueNexus = null, redNexus = null;
        foreach (var s in Object.FindObjectsByType<Structure>(FindObjectsSortMode.None))
        {
            if (s.GetComponent<Twisted3v3.Match.NexusController>() == null) continue;
            if (s.Team == Team.Blue) blueNexus = s.transform.position;
            else if (s.Team == Team.Red) redNexus = s.transform.position;
        }
        if (blueNexus == null || redNexus == null)
        {
            Debug.LogError("[Lanes] Nexus bleu/rouge introuvables — abandon.");
            return;
        }

        int rebuilt = 0;
        foreach (var spawner in Object.FindObjectsByType<Twisted3v3.Minions.MinionWaveSpawner>(FindObjectsSortMode.None))
        {
            string n = spawner.name.ToLowerInvariant();
            bool blue = n.Contains("blue");
            float laneX = n.Contains("left") ? -18f : 18f;
            Vector3 ownNexus = blue ? blueNexus.Value : redNexus.Value;
            Vector3 enemyNexus = blue ? redNexus.Value : blueNexus.Value;
            float sideSign = blue ? 1f : -1f; // sens de progression sur l'axe Z

            // Apparition au Nexus (décalé vers la lane pour sortir du carve NavMesh).
            Vector3 spawnPos = ownNexus + new Vector3(Mathf.Sign(laneX) * 2.5f, 0f, sideSign * 2f);
            spawnPos.y = 0f;
            spawner.transform.position = spawnPos;

            // Chemin : entrée de lane → bout de lane → Nexus ennemi (la tour est dessus).
            for (int i = spawner.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(spawner.transform.GetChild(i).gameObject);
            CreateWaypoint(spawner.transform, 0, new Vector3(laneX, 0f, sideSign * -22f));
            CreateWaypoint(spawner.transform, 1, new Vector3(laneX, 0f, sideSign * 22f));
            CreateWaypoint(spawner.transform, 2, new Vector3(enemyNexus.x, 0f, enemyNexus.z));

            // Fréquence doublée sur l'instance (écrase la valeur sérialisée).
            var so = new SerializedObject(spawner);
            so.FindProperty("_waveInterval").floatValue = 12f;
            so.FindProperty("_firstWaveDelay").floatValue = 5f;
            so.ApplyModifiedProperties();

            rebuilt++;
            Debug.Log($"[Lanes] {spawner.name} : spawn au Nexus {(blue ? "bleu" : "rouge")}, lane x={laneX}, vague/12 s.");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[Lanes] {rebuilt} spawner(s) reconstruits — scène sauvegardée.");
    }

    private static void CreateWaypoint(Transform parent, int index, Vector3 position)
    {
        var wp = new GameObject($"WP_{index}");
        wp.transform.SetParent(parent, false);
        wp.transform.position = position;
    }

    private static string Fmt(Vector3 v) => $"({v.x:0.#}, {v.y:0.#}, {v.z:0.#})";
}
