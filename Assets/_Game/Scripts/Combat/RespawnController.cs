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
        private ChampionVisuals _visuals;       // source de vérité des renderers
        private Collider[] _colliders;
        private Vector3 _spawnPosition;
        private bool _spawnOverridden; // position imposée (ex: fontaine)

        public bool IsRespawning { get; private set; }

        /// <summary>
        /// Impose la position de réapparition (prioritaire sur le _spawnPoint sérialisé).
        /// Utilisé par le <see cref="FountainSpawner"/> pour respawn = fontaine.
        /// Fonctionne qu'il soit appelé avant ou après Start.
        /// </summary>
        public void SetSpawnPosition(Vector3 position)
        {
            _spawnPosition = position;
            _spawnOverridden = true;
        }

        private void Awake()
        {
            _champion = GetComponent<Champion>();
            _bounty = GetComponent<KillReward>();
            _visuals = ChampionVisuals.Of(_champion); // ajouté tôt : capture au Start
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
            // Ne pas écraser une position déjà imposée (fontaine) par le _spawnPoint sérialisé.
            if (!_spawnOverridden)
                _spawnPosition = _spawnPoint != null ? _spawnPoint.position : transform.position;
        }

        private void HandleDeath(Champion champion)
        {
            // Rafraîchit les caches : le sélecteur de champion peut avoir ajouté/retiré
            // le PlayerController après notre Awake. Les renderers sont gérés par
            // ChampionVisuals (état de référence, capsule masquée comprise).
            _colliders = GetComponentsInChildren<Collider>(true);
            _controlBehaviours = new Behaviour[]
            {
                GetComponent<Player.PlayerController>(), GetComponent<AutoAttack>()
            };

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
            // Restaure l'état de référence des renderers (et non « tout activer » :
            // la capsule masquée sous un modèle 3D doit rester masquée au respawn).
            if (_visuals != null) _visuals.SetVisible(active);
            foreach (var c in _colliders) if (c != null) c.enabled = active;
        }
    }
}
