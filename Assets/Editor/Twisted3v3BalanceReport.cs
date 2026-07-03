using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Twisted3v3.Abilities;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Stats;

/// <summary>
/// Rapport d'équilibrage *headless* : lit les assets de champions et de capacités,
/// estime le burst théorique (somme des dégâts de base au rang max + ratios × stats
/// de référence) et le DPS d'auto-attaque, puis imprime une table triée en signalant
/// les valeurs aberrantes (> 1.4× ou < 0.65× la médiane). Sert à tuner sans build.
/// Lancer via Tools ▸ Twisted3v3 ▸ Balance Report.
/// </summary>
public static class Twisted3v3BalanceReport
{
    // Stats de référence « fin de partie » pour matérialiser les ratios AD/AP.
    private const float SampleAd = 280f;
    private const float SampleAp = 260f;

    private struct Row
    {
        public string Name;
        public ChampionRole Role;
        public float Burst;   // dégâts de combo théorique (rang max)
        public float AutoDps; // DPS d'auto-attaque au niveau 18
    }

    [MenuItem("Tools/Twisted3v3/Balance Report")]
    public static void Report()
    {
        var rows = new List<Row>();
        foreach (var guid in AssetDatabase.FindAssets("t:ChampionData"))
        {
            var champ = AssetDatabase.LoadAssetAtPath<ChampionData>(AssetDatabase.GUIDToAssetPath(guid));
            if (champ == null) continue;

            float burst = AbilityBurst(champ.Q) + AbilityBurst(champ.Z)
                        + AbilityBurst(champ.E) + AbilityBurst(champ.R);

            rows.Add(new Row
            {
                Name = champ.DisplayName,
                Role = champ.Role,
                Burst = burst,
                AutoDps = AutoAttackDps(champ)
            });
        }

        if (rows.Count == 0) { Debug.LogWarning("[Balance] Aucun ChampionData trouvé."); return; }

        float medBurst = Median(rows.Select(r => r.Burst));
        float medDps = Median(rows.Select(r => r.AutoDps));

        var sb = new StringBuilder();
        sb.AppendLine("========== RAPPORT D'ÉQUILIBRAGE Twisted3v3 ==========");
        sb.AppendLine($"(réf : AD={SampleAd}, AP={SampleAp} ; burst = combo rang max ; DPS auto = niv.18)");
        sb.AppendLine($"Médianes → burst {medBurst:0} | DPS auto {medDps:0}\n");
        sb.AppendLine($"{"Champion",-12} {"Rôle",-9} {"Burst",8} {"DPS",7}  Flags");
        sb.AppendLine(new string('-', 52));

        foreach (var r in rows.OrderByDescending(r => r.Burst))
        {
            string flags = "";
            if (r.Burst > medBurst * 1.4f) flags += "⚠ burst haut ";
            else if (r.Burst < medBurst * 0.65f) flags += "⚠ burst bas ";
            if (r.AutoDps > medDps * 1.4f) flags += "⚠ DPS haut ";
            else if (r.AutoDps < medDps * 0.65f) flags += "⚠ DPS bas ";

            sb.AppendLine($"{r.Name,-12} {r.Role,-9} {r.Burst,8:0} {r.AutoDps,7:0}  {flags}");
        }
        sb.AppendLine(new string('-', 52));
        sb.AppendLine("⚠ = à revoir. Ajuste les *ByRank / ratios directement dans les .asset (aucun build requis).");

        Debug.Log(sb.ToString());
    }

    /// <summary>Estime le burst d'une capacité : Σ(dégâts de base rang max) + Σ(ratios × réf).</summary>
    private static float AbilityBurst(AbilityData ability)
    {
        if (ability == null) return 0f;
        int maxRank = Mathf.Max(1, ability.MaxRank);
        float flat = 0f, adRatio = 0f, apRatio = 0f;

        foreach (var f in ability.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (f.FieldType == typeof(float[]) && f.Name.Contains("Damage"))
            {
                if (f.GetValue(ability) is float[] arr && arr.Length > 0)
                    flat += arr[Mathf.Clamp(maxRank - 1, 0, arr.Length - 1)];
            }
            else if (f.FieldType == typeof(float) && f.Name.Contains("Ratio"))
            {
                float v = (float)f.GetValue(ability);
                if (f.Name.Contains("Ap")) apRatio += v;
                else adRatio += v; // AdRatio par défaut
            }
        }
        return flat + adRatio * SampleAd + apRatio * SampleAp;
    }

    /// <summary>DPS d'auto-attaque au niveau 18 = AD(18) × AttackSpeed.</summary>
    private static float AutoAttackDps(ChampionData champ)
    {
        float ad = StatAtLevel(champ, StatType.AttackDamage, 18);
        float atkSpeed = StatAtLevel(champ, StatType.AttackSpeed, 18);
        return ad * atkSpeed;
    }

    private static float StatAtLevel(ChampionData champ, StatType type, int level)
    {
        foreach (var e in champ.BaseStats)
            if (e.Stat == type) return e.BaseValue + e.PerLevel * (level - 1);
        return 0f;
    }

    private static float Median(IEnumerable<float> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        if (sorted.Count == 0) return 0f;
        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) * 0.5f : sorted[mid];
    }
}
