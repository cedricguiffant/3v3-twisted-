using UnityEngine;
using UnityEngine.AI;
using Twisted3v3.Core;
using Twisted3v3.Combat;

namespace Twisted3v3.Minions
{
    /// <summary>
    /// Sbire de lane. Avance le long d'un chemin (waypoints) vers la base ennemie ;
    /// si un ennemi (sbire/champion/bâtiment adverse) entre dans son rayon d'aggro,
    /// il s'arrête pour l'attaquer puis reprend sa route. Donne or/XP au last-hit.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Minion : MonoBehaviour, IDamageable, IHealthInfo
    {
        private Team _team;
        private float _maxHealth, _health, _damage;
        private float _attackInterval = 1f, _attackRange = 2f, _aggroRange = 6.5f;

        private Vector3[] _path;
        private int _waypoint;
        private NavMeshAgent _agent;
        private IDamageable _target;
        private object _lastDamageSource;
        private float _cooldown;
        private KillReward _reward;
        private LayerMask _enemyMask = ~0;

        private static readonly Collider[] _buffer = new Collider[32];

        public Team Team => _team;
        public Transform Transform => transform;
        public bool IsDead => _health <= 0f;
        public float CurrentHealth => _health;
        public float MaxHealth => _maxHealth;

        public void Initialize(Team team, Vector3[] path, float maxHealth, float damage,
            float moveSpeed, Color color, int gold, float xp, LayerMask enemyMask)
        {
            _team = team;
            _path = path;
            _maxHealth = maxHealth; _health = maxHealth; _damage = damage;
            _enemyMask = enemyMask;

            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = moveSpeed; _agent.radius = 0.35f; _agent.height = 1.6f;
            _agent.angularSpeed = 720f; _agent.acceleration = 30f;
            _agent.stoppingDistance = _attackRange * 0.8f;
            _agent.avoidancePriority = Random.Range(40, 60);
            if (NavMesh.SamplePosition(transform.position, out var hit, 5f, NavMesh.AllAreas))
                _agent.Warp(hit.position);

            _reward = gameObject.AddComponent<KillReward>();
            _reward.Gold = gold; _reward.Experience = xp;

            var bar = gameObject.AddComponent<Twisted3v3.UI.WorldHealthBar>();
            bar.SetColor(color); bar.SetOffsetY(1.4f); bar.SetWidth(80f);
            var rend = GetComponent<Renderer>(); if (rend != null) rend.material.color = color;
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead) return;
            _lastDamageSource = info.Source;
            _health -= info.Amount;
            if (IsDead) Die();
        }

        public void Heal(float amount, object source = null) =>
            _health = Mathf.Min(_maxHealth, _health + amount);

        private void Update()
        {
            if (IsDead || _agent == null || !_agent.isOnNavMesh) return;
            if (_cooldown > 0f) _cooldown -= Time.deltaTime;

            // Cible courante encore valide ?
            if (_target != null && (_target.IsDead ||
                (_target.Transform.position - transform.position).sqrMagnitude > _aggroRange * _aggroRange))
                _target = null;
            if (_target == null) _target = FindEnemy();

            if (_target != null) FightTarget();
            else Advance();
        }

        private void FightTarget()
        {
            float dist = Vector3.Distance(transform.position, _target.Transform.position);
            if (dist > _attackRange)
            {
                _agent.isStopped = false;
                _agent.SetDestination(_target.Transform.position);
            }
            else
            {
                _agent.isStopped = true;
                if (_cooldown <= 0f)
                {
                    _cooldown = _attackInterval;
                    _target.TakeDamage(new DamageInfo(_damage, DamageType.Physical, this));
                }
            }
        }

        private void Advance()
        {
            _agent.isStopped = false;
            if (_path == null || _waypoint >= _path.Length) return;

            _agent.SetDestination(_path[_waypoint]);
            if ((transform.position - _path[_waypoint]).sqrMagnitude < 2.25f)
                _waypoint++;
        }

        private IDamageable FindEnemy()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _aggroRange, _buffer, _enemyMask);
            IDamageable best = null;
            float bestSq = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (!_buffer[i].TryGetComponent<IDamageable>(out var d)) continue;
                if (d.IsDead) continue;
                // Ennemis Bleu/Rouge uniquement (on ignore la jungle neutre).
                if (d.Team == _team || (d.Team != Team.Blue && d.Team != Team.Red)) continue;
                float sq = (d.Transform.position - transform.position).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = d; }
            }
            return best;
        }

        private void Die()
        {
            KillRewardService.Award(_lastDamageSource as Twisted3v3.Champions.Champion, _reward);
            Destroy(gameObject);
        }
    }
}
