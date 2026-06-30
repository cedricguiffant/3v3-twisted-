using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Stats;

namespace Twisted3v3.Jungle
{
    /// <summary>
    /// Applique des buffs (CampReward) à un champion ou à toute une équipe pour une
    /// durée, puis les retire proprement (via la source partagée du StatModifier).
    /// Singleton auto-créé : aucun setup en scène.
    /// </summary>
    public sealed class TeamBuffRunner : MonoBehaviour
    {
        private static TeamBuffRunner _instance;

        public static TeamBuffRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("TeamBuffRunner");
                    _instance = go.AddComponent<TeamBuffRunner>();
                }
                return _instance;
            }
        }

        public void ApplyToTeam(Team team, IReadOnlyList<CampReward> rewards, float duration)
        {
            var members = new List<Champion>();
            foreach (var c in Object.FindObjectsByType<Champion>(FindObjectsSortMode.None))
                if (c.Team == team) members.Add(c);
            ApplyTo(members, rewards, duration);
        }

        public void ApplyToChampion(Champion champion, IReadOnlyList<CampReward> rewards, float duration)
            => ApplyTo(new List<Champion> { champion }, rewards, duration);

        private void ApplyTo(List<Champion> champions, IReadOnlyList<CampReward> rewards, float duration)
        {
            if (rewards == null || rewards.Count == 0 || champions.Count == 0) return;

            // Source unique pour ce buff → retrait ciblé plus tard.
            object source = new object();
            foreach (var champ in champions)
            {
                if (champ == null) continue;
                foreach (var r in rewards)
                    champ.Stats.AddModifier(r.Stat, new StatModifier(r.Value, ModifierType.Flat, source));
            }
            StartCoroutine(RemoveAfter(champions, source, duration));
        }

        private IEnumerator RemoveAfter(List<Champion> champions, object source, float duration)
        {
            yield return new WaitForSeconds(duration);
            foreach (var champ in champions)
                if (champ != null) champ.Stats.RemoveAllFromSource(source);
        }
    }
}
