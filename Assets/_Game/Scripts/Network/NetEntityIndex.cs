using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Twisted3v3.Champions;
using Twisted3v3.Combat;

namespace Twisted3v3.Net
{
    /// <summary>
    /// Table d'identifiants réseau déterministes pour les entités de la scène de
    /// match. Hôte et clients chargent la même scène : trier les champions et les
    /// structures par nom (puis position pour départager) donne les mêmes ids des
    /// deux côtés, sans rien baker dans la scène.
    /// </summary>
    public sealed class NetEntityIndex
    {
        public const int KindNone = 0;
        public const int KindChampion = 1;
        public const int KindStructure = 2;

        public List<Champion> Champions = new();
        public List<Structure> Structures = new();

        public static NetEntityIndex Build()
        {
            var index = new NetEntityIndex
            {
                Champions = Object.FindObjectsByType<Champion>(FindObjectsSortMode.None)
                    .OrderBy(c => c.name)
                    .ThenBy(c => c.transform.position.x)
                    .ThenBy(c => c.transform.position.z)
                    .ToList(),
                Structures = Object.FindObjectsByType<Structure>(FindObjectsSortMode.None)
                    .OrderBy(s => s.name)
                    .ThenBy(s => s.transform.position.x)
                    .ThenBy(s => s.transform.position.z)
                    .ToList()
            };
            return index;
        }

        public int IdOf(Champion champion) => Champions.IndexOf(champion);

        public IDamageable Resolve(int kind, int id)
        {
            switch (kind)
            {
                case KindChampion:
                    return id >= 0 && id < Champions.Count ? Champions[id] : null;
                case KindStructure:
                    return id >= 0 && id < Structures.Count ? Structures[id] : null;
                default:
                    return null;
            }
        }

        public (int kind, int id) IdOfDamageable(IDamageable target)
        {
            if (target is Champion c)
            {
                int id = Champions.IndexOf(c);
                if (id >= 0) return (KindChampion, id);
            }
            else if (target is Structure s)
            {
                int id = Structures.IndexOf(s);
                if (id >= 0) return (KindStructure, id);
            }
            return (KindNone, -1);
        }

        /// <summary>Même règle de correspondance que le PlayerChampionBinder.</summary>
        public Champion FindChampionByName(string wanted)
        {
            if (string.IsNullOrWhiteSpace(wanted)) return null;
            foreach (var c in Champions)
            {
                if (c.Data != null && string.Equals(c.Data.DisplayName, wanted,
                        System.StringComparison.OrdinalIgnoreCase)) return c;
                if (c.name.ToLowerInvariant().Contains(wanted.ToLowerInvariant())) return c;
            }
            return null;
        }
    }
}
