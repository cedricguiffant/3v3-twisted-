using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Twisted3v3.Stats;

namespace Twisted3v3.Champions
{
    /// <summary>
    /// Déplacement d'un champion via <see cref="NavMeshAgent"/> : pathfinding autour
    /// des murs pour les ordres normaux, et déplacements forcés (dash, attraction,
    /// repoussée) intégrés avec <c>agent.Move</c> pour respecter les bords du NavMesh
    /// (un dash s'arrête contre un mur au lieu de le traverser).
    /// </summary>
    [RequireComponent(typeof(Champion))]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class ChampionMotor : MonoBehaviour
    {
        private Champion _champion;
        private NavMeshAgent _agent;
        private Coroutine _forcedMovement;

        /// <summary>Vrai pendant un déplacement forcé (dash/pull) — bloque l'input.</summary>
        public bool IsBeingForced => _forcedMovement != null;

        private void Awake()
        {
            _champion = GetComponent<Champion>();
            _agent = GetComponent<NavMeshAgent>();
            _agent.angularSpeed = 720f;
            _agent.acceleration = 40f;
            _agent.stoppingDistance = 0.1f;
            _agent.autoBraking = true;
        }

        private void Update()
        {
            if (_agent == null || !_agent.isOnNavMesh) return;

            if (_champion.Stats != null)
                _agent.speed = _champion.Stats.Value(StatType.MoveSpeed);

            // Stoppe la navigation si mort / sous CC bloquant / déplacement forcé.
            _agent.isStopped = _champion.IsDead || _champion.IsStunned
                               || _champion.IsRooted || IsBeingForced;
        }

        /// <summary>Ordre de déplacement standard (clic droit) — suit un chemin NavMesh.</summary>
        public void MoveTo(Vector3 worldPoint)
        {
            if (!CanMove || !_agent.isOnNavMesh) return;
            _agent.isStopped = false;
            _agent.SetDestination(worldPoint);
        }

        public void Stop()
        {
            if (_agent.isOnNavMesh) _agent.ResetPath();
        }

        /// <summary>Téléporte proprement l'agent sur le NavMesh (respawn).</summary>
        public void Warp(Vector3 position)
        {
            if (_agent != null) _agent.Warp(position);
            else transform.position = position;
        }

        private bool CanMove => !_champion.IsDead && !_champion.IsStunned
                                && !_champion.IsRooted && !IsBeingForced;

        // --- Déplacements forcés ---
        public void Dash(Vector3 destination, float speed) => StartForced(destination, speed);

        public void PullToward(Vector3 source, float distance, float speed)
        {
            Vector3 dir = source - transform.position; dir.y = 0f; dir.Normalize();
            StartForced(transform.position + dir * distance, speed);
        }

        public void Knockback(Vector3 source, float distance, float speed)
        {
            Vector3 dir = transform.position - source; dir.y = 0f; dir.Normalize();
            StartForced(transform.position + dir * distance, speed);
        }

        private void StartForced(Vector3 destination, float speed)
        {
            if (_forcedMovement != null) StopCoroutine(_forcedMovement);
            _forcedMovement = StartCoroutine(ForcedMove(destination, speed));
        }

        private IEnumerator ForcedMove(Vector3 destination, float speed)
        {
            if (_agent.isOnNavMesh) { _agent.ResetPath(); _agent.isStopped = true; }
            destination.y = transform.position.y;

            // Timeout : si un mur bloque, on n'attend pas indéfiniment.
            float maxTime = Vector3.Distance(transform.position, destination)
                            / Mathf.Max(0.1f, speed) + 0.3f;
            float elapsed = 0f;

            while ((transform.position - destination).sqrMagnitude > 0.09f && elapsed < maxTime)
            {
                elapsed += Time.deltaTime;
                Vector3 next = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
                Vector3 delta = next - transform.position;
                if (_agent.isOnNavMesh) _agent.Move(delta); // clampé par le NavMesh
                else transform.position = next;
                yield return null;
            }
            _forcedMovement = null;
        }
    }
}
