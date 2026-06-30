using System.Collections;
using UnityEngine;
using Twisted3v3.Core;

namespace Twisted3v3.Minions
{
    /// <summary>
    /// Fait apparaître des vagues de sbires pour une équipe le long d'une lane.
    /// Le chemin est défini par les GameObjects enfants (waypoints, dans l'ordre).
    /// </summary>
    public sealed class MinionWaveSpawner : MonoBehaviour
    {
        [SerializeField] private Team _team = Team.Blue;
        [SerializeField] private int _minionsPerWave = 4;
        [SerializeField] private float _firstWaveDelay = 6f;
        [SerializeField] private float _waveInterval = 25f;
        [SerializeField] private float _spawnSpacing = 0.7f;

        [Header("Stats des sbires")]
        [SerializeField] private float _health = 220f;
        [SerializeField] private float _damage = 16f;
        [SerializeField] private float _moveSpeed = 3.6f;
        [SerializeField] private float _scale = 0.8f;
        [SerializeField] private int _goldPerMinion = 20;
        [SerializeField] private float _xpPerMinion = 30f;
        [SerializeField] private int _unitLayer = 10;

        private Vector3[] _path;
        private float _timer;

        private void Start()
        {
            int n = transform.childCount;
            _path = new Vector3[n];
            for (int i = 0; i < n; i++) _path[i] = transform.GetChild(i).position;
            _timer = _firstWaveDelay;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _waveInterval;
                StartCoroutine(SpawnWave());
            }
        }

        private IEnumerator SpawnWave()
        {
            for (int i = 0; i < _minionsPerWave; i++)
            {
                SpawnMinion();
                yield return new WaitForSeconds(_spawnSpacing);
            }
        }

        private void SpawnMinion()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = _team + "_Minion";
            go.layer = _unitLayer;
            go.transform.position = transform.position + Vector3.up;
            go.transform.localScale = Vector3.one * _scale;

            Color color = _team == Team.Blue
                ? new Color(0.35f, 0.55f, 0.95f)
                : new Color(0.95f, 0.45f, 0.4f);

            var minion = go.AddComponent<Minion>();
            minion.Initialize(_team, _path, _health, _damage, _moveSpeed, color,
                              _goldPerMinion, _xpPerMinion, 1 << _unitLayer);
        }
    }
}
