using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Combat;

namespace Twisted3v3.Match
{
    /// <summary>
    /// Nexus d'une équipe : sa destruction donne la victoire à l'équipe adverse.
    /// Reste invulnérable tant qu'au moins une de ses tours gardiennes tient debout.
    /// </summary>
    [RequireComponent(typeof(Structure))]
    public sealed class NexusController : MonoBehaviour
    {
        [Tooltip("Tours qui protègent le Nexus : il est invulnérable tant qu'une tient.")]
        [SerializeField] private Structure[] _guardians;

        private Structure _structure;

        private void Awake()
        {
            _structure = GetComponent<Structure>();
            _structure.OnDestroyed += HandleDestroyed;
        }

        private void OnDestroy()
        {
            if (_structure != null) _structure.OnDestroyed -= HandleDestroyed;
        }

        public void SetGuardians(Structure[] guardians) => _guardians = guardians;

        private void Update()
        {
            _structure.IsInvulnerable = AnyGuardianAlive();
        }

        private bool AnyGuardianAlive()
        {
            if (_guardians == null) return false;
            foreach (var g in _guardians)
                if (g != null && !g.IsDead) return true;
            return false;
        }

        private void HandleDestroyed(Structure s)
        {
            Team winner = _structure.Team == Team.Blue ? Team.Red : Team.Blue;
            if (MatchManager.Instance != null) MatchManager.Instance.RegisterNexusDestroyed(winner);
        }
    }
}
