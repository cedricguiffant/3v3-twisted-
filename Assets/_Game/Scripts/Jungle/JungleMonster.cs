using System;
using UnityEngine;
using UnityEngine.AI;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Combat;

namespace Twisted3v3.Jungle
{
    /// <summary>
    /// Monstre de jungle neutre. Reste à son camp, riposte quand on l'attaque,
    /// poursuit l'agresseur dans une limite (leash) puis se régénère s'il décroche.
    /// Implémente <see cref="IDamageable"/> : ciblable par auto-attaques et sorts.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class JungleMonster : MonoBehaviour, IDamageable, IHealthInfo
    {
        private float _maxHealth = 800f;
        private float _health;
        private float _contactDamage = 25f;
        private float _attackInterval = 1.2f;
        private float _moveSpeed = 3f;
        private float _leashRange = 10f;
        private const float AttackRange = 2.5f;

        private Vector3 _home;
        private IDamageable _aggro;
        private object _lastDamageSource;
        private float _attackCooldown;
        private KillReward _reward;
        private NavMeshAgent _agent;

        public Team Team => Team.Neutral;
        public Transform Transform => transform;
        public bool IsDead => _health <= 0f;

        // IHealthInfo (barre de vie flottante)
        public float CurrentHealth => _health;
        public float MaxHealth => _maxHealth;

        /// <summary>(monstre, tueur) — tueur null si mort sans champion (reset).</summary>
        public event Action<JungleMonster, Champion> OnDied;

        public void Initialize(JungleCampData data)
        {
            _maxHealth = data.MonsterMaxHealth;
            _health = _maxHealth;
            _contactDamage = data.MonsterContactDamage;
            _attackInterval = data.MonsterAttackInterval;
            _moveSpeed = data.MonsterMoveSpeed;
            _leashRange = data.LeashRange;
            _home = transform.position;

            _reward = GetComponent<KillReward>() ?? gameObject.AddComponent<KillReward>();
            _reward.Gold = data.GoldPerMonster;
            _reward.Experience = data.XpPerMonster;

            // Agent de navigation (pathfinding autour des murs).
            // Pattern explicite : l'opérateur ?? ne respecte pas le null Unity.
            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null) _agent = gameObject.AddComponent<NavMeshAgent>();
            _agent.radius = 0.5f;
            _agent.height = 2f;
            _agent.speed = _moveSpeed;
            _agent.angularSpeed = 720f;
            _agent.acceleration = 30f;
            _agent.stoppingDistance = AttackRange * 0.8f;

            // Se cale sur le NavMesh le plus proche (le spawn peut être légèrement au-dessus).
            if (NavMesh.SamplePosition(transform.position, out var hit, 5f, NavMesh.AllAreas))
                _agent.Warp(hit.position);

            // Barre de vie flottante (orange pour les monstres neutres).
            var bar = gameObject.AddComponent<Twisted3v3.UI.WorldHealthBar>();
            bar.SetColor(new Color(0.9f, 0.55f, 0.15f));
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead) return;
            _lastDamageSource = info.Source;
            _health -= info.Amount;

            // S'aggro sur le champion qui l'attaque.
            if (info.Source is Champion attacker && !attacker.IsDead)
                _aggro = attacker;

            if (IsDead) Die();
        }

        public void Heal(float amount, object source = null) =>
            _health = Mathf.Min(_maxHealth, _health + amount);

        private void Update()
        {
            if (IsDead) return;
            if (_attackCooldown > 0f) _attackCooldown -= Time.deltaTime;
            bool onMesh = _agent != null && _agent.isOnNavMesh;

            // Pas de cible valide → retour au camp.
            if (_aggro == null || _aggro.IsDead)
            {
                _aggro = null;
                if (onMesh && (transform.position - _home).sqrMagnitude > 0.25f)
                    _agent.SetDestination(_home);
                return;
            }

            // Décrochage si le monstre s'est trop éloigné de son camp.
            if ((transform.position - _home).sqrMagnitude > _leashRange * _leashRange)
            {
                _aggro = null;
                _health = _maxHealth; // régénération au reset
                if (onMesh) _agent.SetDestination(_home);
                return;
            }

            float dist = Vector3.Distance(transform.position, _aggro.Transform.position);
            if (dist > AttackRange)
            {
                if (onMesh) _agent.SetDestination(_aggro.Transform.position);
            }
            else
            {
                if (onMesh) _agent.ResetPath();
                if (_attackCooldown <= 0f)
                {
                    _attackCooldown = _attackInterval;
                    _aggro.TakeDamage(new DamageInfo(_contactDamage, DamageType.Physical, this));
                }
            }
        }

        private void Die()
        {
            var killer = _lastDamageSource as Champion;
            KillRewardService.Award(killer, _reward); // or + XP au tueur
            OnDied?.Invoke(this, killer);
            Destroy(gameObject);
        }
    }
}
