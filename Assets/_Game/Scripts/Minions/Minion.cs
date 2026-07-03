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
        private Vector3 _pathJitter; // décalage latéral propre au sbire (colonne fluide)
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
            // Chaque sbire vise un point légèrement décalé du waypoint : la vague
            // s'étale en colonne au lieu de se compacter sur un point unique.
            _pathJitter = new Vector3(Random.Range(-1.6f, 1.6f), 0f, Random.Range(-1.2f, 1.2f));

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
            gameObject.AddComponent<Twisted3v3.VFX.MaterialCleanup>(); // libère la copie de matériau
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

            // Cible courante encore valide ? (morte, hors d'aggro, ou devenue invulnérable)
            if (_target != null && (_target.IsDead
                || (_target is Structure ts && ts.IsInvulnerable)
                || (_target.Transform.position - transform.position).sqrMagnitude > _aggroRange * _aggroRange))
                _target = null;
            if (_target == null) _target = FindEnemy();

            if (_target != null) FightTarget();
            else Advance();
        }

        private void FightTarget()
        {
            // En combat : on s'arrête à portée de frappe (pas en marche de lane).
            _agent.stoppingDistance = _attackRange * 0.8f;

            // Distance au point le plus proche du collider : indispensable pour les
            // gros bâtiments (le centre d'un Nexus est hors de portée de frappe).
            Vector3 targetPoint = _target.Transform.position;
            if (_target.Transform.TryGetComponent<Collider>(out var col))
                targetPoint = col.ClosestPoint(transform.position);

            float dist = Vector3.Distance(transform.position, targetPoint);
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
            // En marche : pas d'arrêt anticipé (un stoppingDistance > rayon de passage
            // figeait la colonne : l'agent s'arrêtait à 1.6 u d'un waypoint validé à 1.5 u).
            _agent.stoppingDistance = 0f;
            if (_path == null || _waypoint >= _path.Length) return;

            Vector3 waypoint = _path[_waypoint] + _pathJitter;
            _agent.SetDestination(waypoint);
            // Rayon de passage large (3.5 u) : la vague glisse d'un waypoint au suivant
            // sans que chacun doive atteindre le point exact (fin des embouteillages).
            if ((transform.position - waypoint).sqrMagnitude < 12.25f)
                _waypoint++;
        }

        /// <summary>
        /// Meilleure cible dans le rayon d'aggro, par priorité de lane : sbires ennemis
        /// d'abord, puis bâtiments attaquables (la tour tombe avant le Nexus grâce à
        /// son invulnérabilité), puis champions. Ignore la jungle neutre.
        /// </summary>
        private IDamageable FindEnemy()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _aggroRange, _buffer, _enemyMask);
            IDamageable minion = null, structure = null, champion = null;
            float minionSq = float.MaxValue, structSq = float.MaxValue, champSq = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (!_buffer[i].TryGetComponent<IDamageable>(out var d)) continue;
                if (d.IsDead) continue;
                // Ennemis Bleu/Rouge uniquement (on ignore la jungle neutre).
                if (d.Team == _team || (d.Team != Team.Blue && d.Team != Team.Red)) continue;
                // Nexus protégé par sa tour : inattaquable, on ne s'y bloque pas.
                if (d is Structure s && s.IsInvulnerable) continue;

                float sq = (d.Transform.position - transform.position).sqrMagnitude;
                if (d is Minion) { if (sq < minionSq) { minionSq = sq; minion = d; } }
                else if (d is Structure) { if (sq < structSq) { structSq = sq; structure = d; } }
                else if (sq < champSq) { champSq = sq; champion = d; }
            }
            return minion ?? structure ?? champion;
        }

        private void Die()
        {
            KillRewardService.Award(_lastDamageSource as Twisted3v3.Champions.Champion, _reward);
            Destroy(gameObject);
        }
    }
}
