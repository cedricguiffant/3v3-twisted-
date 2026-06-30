using System;
using UnityEngine;
using Twisted3v3.Core;

namespace Twisted3v3.Match
{
    /// <summary>
    /// État de la partie : score de kills par équipe, chrono et condition de victoire
    /// (premier à N kills, ou meilleur score à la fin du temps). Met le jeu en pause
    /// (timeScale 0) à la fin. Composant de scène ; accessible via <see cref="Instance"/>.
    /// </summary>
    public sealed class MatchManager : MonoBehaviour
    {
        public static MatchManager Instance { get; private set; }

        [SerializeField] private int _killsToWin = 10;
        [Tooltip("Durée max en secondes (0 = pas de limite de temps).")]
        [SerializeField] private float _matchDuration = 900f;

        public int BlueKills { get; private set; }
        public int RedKills { get; private set; }
        public float Elapsed { get; private set; }
        public int KillsToWin => _killsToWin;
        public float MatchDuration => _matchDuration;
        public bool IsEnded { get; private set; }
        public Team Winner { get; private set; } = Team.None;

        public event Action OnScoreChanged;
        public event Action OnMatchEnded;

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f; // réinitialise après un éventuel écran de fin précédent
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (IsEnded || _matchDuration <= 0f) return;
            Elapsed += Time.deltaTime;
            if (Elapsed >= _matchDuration)
                End(BlueKills > RedKills ? Team.Blue : RedKills > BlueKills ? Team.Red : Team.None);
        }

        /// <summary>Enregistre un kill de champion pour l'équipe du tueur.</summary>
        public void RegisterKill(Team killerTeam)
        {
            if (IsEnded) return;
            if (killerTeam == Team.Blue) BlueKills++;
            else if (killerTeam == Team.Red) RedKills++;
            else return;

            OnScoreChanged?.Invoke();

            if (BlueKills >= _killsToWin) End(Team.Blue);
            else if (RedKills >= _killsToWin) End(Team.Red);
        }

        /// <summary>Destruction d'un Nexus → victoire immédiate de l'équipe adverse.</summary>
        public void RegisterNexusDestroyed(Team winner)
        {
            if (IsEnded) return;
            End(winner);
        }

        private void End(Team winner)
        {
            IsEnded = true;
            Winner = winner;
            Time.timeScale = 0f; // fige la partie
            OnMatchEnded?.Invoke();
        }
    }
}
