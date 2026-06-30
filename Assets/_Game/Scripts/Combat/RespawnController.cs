using System.Collections;
using UnityEngine;
using Twisted3v3.Champions;

namespace Twisted3v3.Combat
{
    /// <summary>
    /// Gère la mort et la réapparition d'un champion : désactive le contrôle et le
    /// visuel à la mort, attribue le kill au dernier attaquant, attend le timer puis
    /// ressuscite au point de spawn. Le temps de respawn croît avec le niveau.
    /// </summary>
    [RequireComponent(typeof(Champion))]
    public sealed class RespawnController : MonoBehaviour
    {
        [Tooltip("Transform du point de réapparition. Si vide, la position de départ est utilisée.")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private float _baseRespawnTime = 5f;
        [SerializeField] private float _respawnTimePerLevel = 0.5f;

        private Champion _champion;
        private KillReward _bounty;
        private Behaviour[] _controlBehaviours; // désactivés pendant la mort
        private Renderer[] _renderers;
        private Collider[] _colliders;
        private Vector3 _spawnPosition;

        public bool IsRespawning { get; private set; }

        private void Awake()
        {
            _champion = GetComponent<Champion>();
            _bounty = GetComponent<KillReward>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider>(true);

            // Comportements à couper pendant la mort (input, attaque).
            var pc = GetComponent<Player.PlayerController>();
            var aa = GetComponent<AutoAttack>();
            _controlBehaviours = new Behaviour[] { pc, aa };
        }

        private void OnEnable() => _champion.OnDied += HandleDeath;
        private void OnDisable() => _champion.OnDied -= HandleDeath;

        private void Start()
        {
            _spawnPosition = _spawnPoint != null ? _spawnPoint.position : transform.position;
        }

        private void HandleDeath(Champion champion)
        {
            // Attribution du kill au dernier attaquant (or + XP).
            var killer = _champion.Health.LastDamageSource as Champion;
            KillRewardService.Award(_champion.Health.LastDamageSource, _bounty);

            // Score d'équipe (kill de champion par un ennemi).
            if (killer != null && killer.Team != _champion.Team
                && Match.MatchManager.Instance != null)
                Match.MatchManager.Instance.RegisterKill(killer.Team);

            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            IsRespawning = true;
            SetActiveState(false);

            float delay = _baseRespawnTime + _respawnTimePerLevel * _champion.Level;
            Debug.Log($"[Respawn] {_champion.name} mort — réapparition dans {delay:0.0}s");
            yield return new WaitForSeconds(delay);

            _champion.Respawn(_spawnPosition);
            SetActiveState(true);
            IsRespawning = false;
            Debug.Log($"[Respawn] {_champion.name} réapparu.");
        }

        private void SetActiveState(bool active)
        {
            foreach (var b in _controlBehaviours) if (b != null) b.enabled = active;
            foreach (var r in _renderers) if (r != null) r.enabled = active;
            foreach (var c in _colliders) if (c != null) c.enabled = active;
        }
    }
}
