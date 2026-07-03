using System;
using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Stats;

namespace Twisted3v3.Combat
{
    /// <summary>
    /// Attaque de base : poursuit une cible ennemie, l'attaque à la cadence dictée
    /// par la vitesse d'attaque quand elle est à portée. Applique coup critique et
    /// vol de vie. Mêlée par défaut (dégâts instantanés) ; les projectiles viendront
    /// avec les champions à distance.
    /// </summary>
    [RequireComponent(typeof(Champion))]
    public sealed class AutoAttack : MonoBehaviour
    {
        [Tooltip("Marge ajoutée à la portée pour compenser le rayon des capsules.")]
        [SerializeField] private float _rangeBuffer = 1.5f;
        [SerializeField] private float _critMultiplier = 1.75f;

        [Header("Dégâts")]
        [Tooltip("Debug uniquement : ignore l'AttackDamage et inflige une valeur fixe.")]
        [SerializeField] private bool _useFixedDamage = false;
        [SerializeField] private float _fixedDamage = 100f;

        [Header("Acquisition de cible (attaque-déplacement)")]
        [SerializeField] private LayerMask _unitMask = ~0;
        [Tooltip("Rayon de recherche d'un ennemi autour du point cliqué / du champion.")]
        [SerializeField] private float _acquisitionRadius = 6f;

        [Header("À distance (marksman)")]
        [Tooltip("Si vrai, l'auto-attaque tire un projectile à tête chercheuse.")]
        [SerializeField] private bool _ranged = false;
        [SerializeField] private float _projectileSpeed = 20f;
        [SerializeField] private float _projectileRadius = 0.3f;
        [SerializeField] private Color _projectileColor = new(1f, 0.9f, 0.5f);

        private static readonly Collider[] _acquireBuffer = new Collider[32];

        private Champion _champion;
        private IDamageable _target;
        private float _cooldown;
        private float _cooldownDuration = 1f; // durée de la dernière recharge (kiting)

        public IDamageable Target => _target;

        /// <summary>
        /// Kiting : quand actif, le champion recule brièvement après chaque tir puis
        /// se replace pour frapper (« orbe-walk »). Réservé aux duels contre un
        /// champion. Piloté par l'IA (rôles à distance) ou une option joueur.
        /// </summary>
        public bool Kite { get; set; }

        /// <summary>Déclenché à chaque attaque effectuée (passifs : stacks, etc.).</summary>
        public event Action OnAttack;

        /// <summary>Réinitialise le délai d'attaque (ex: « attaque gratuite » de Lirael).</summary>
        public void ResetCooldown() => _cooldown = 0f;

        private void Awake() => _champion = GetComponent<Champion>();

        public void SetTarget(IDamageable target)
        {
            if (target == null || _champion.IsAlly(target)) return;
            _target = target;
        }

        public void ClearTarget() => _target = null;

        /// <summary>
        /// Attaque-déplacement (style MOBA) : cible l'ennemi le plus proche du point
        /// cliqué, sinon le plus proche du champion ; si aucun, se déplace au point.
        /// </summary>
        public void AttackMove(Vector3 worldPoint)
        {
            var enemy = AcquireClosestEnemy(worldPoint) ?? AcquireClosestEnemy(transform.position);
            if (enemy != null)
            {
                SetTarget(enemy);
            }
            else
            {
                ClearTarget();
                _champion.Motor.MoveTo(worldPoint);
            }
        }

        private IDamageable AcquireClosestEnemy(Vector3 center)
        {
            int count = Physics.OverlapSphereNonAlloc(center, _acquisitionRadius, _acquireBuffer, _unitMask);
            IDamageable best = null;
            float bestSq = float.MaxValue;
            Vector3 from = transform.position;
            for (int i = 0; i < count; i++)
            {
                if (!_acquireBuffer[i].TryGetComponent<IDamageable>(out var d)) continue;
                if (d.IsDead || !_champion.IsEnemy(d)) continue;
                float sq = (d.Transform.position - from).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = d; }
            }
            return best;
        }

        private void Update()
        {
            if (_cooldown > 0f) _cooldown -= Time.deltaTime;

            if (_target == null || _target.IsDead) { _target = null; return; }
            if (_champion.IsDead || _champion.IsStunned) return;

            float range = _champion.Stats.Value(StatType.AttackRange) + _rangeBuffer;
            Vector3 toTarget = _target.Transform.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude > range)
            {
                // Hors de portée : on se rapproche.
                _champion.Motor.MoveTo(_target.Transform.position);
                return;
            }

            // À portée : on fait face à la cible.
            if (toTarget.sqrMagnitude > 0.001f) transform.forward = toTarget.normalized;

            if (_cooldown <= 0f) { PerformAttack(); return; }

            // Kiting : recule pendant la première moitié de la recharge (contre un champion),
            // puis se replace pour tirer. Sinon, on tient position.
            bool early = _cooldownDuration > 0f && _cooldown > _cooldownDuration * 0.5f;
            if (Kite && early && _target is Champion)
            {
                Vector3 away = transform.position - _target.Transform.position; away.y = 0f;
                if (away.sqrMagnitude > 0.001f)
                    _champion.Motor.MoveTo(transform.position + away.normalized * 2.5f);
            }
            else
            {
                _champion.Motor.Stop();
            }
        }

        private void PerformAttack()
        {
            float attackSpeed = Mathf.Max(0.1f, _champion.Stats.Value(StatType.AttackSpeed));
            _cooldown = _cooldownDuration = 1f / attackSpeed;

            float damage = _useFixedDamage ? _fixedDamage : _champion.Stats.Value(StatType.AttackDamage);
            bool isCrit = UnityEngine.Random.value < _champion.Stats.Value(StatType.CritChance);
            if (isCrit) damage *= _critMultiplier;

            if (_ranged)
            {
                // Tir d'un projectile à tête chercheuse vers la cible.
                Vector3 origin = transform.position + Vector3.up + transform.forward * 0.6f;
                Vector3 dir = _target.Transform.position - transform.position;
                Projectile.Spawn(_champion, origin, dir, _projectileSpeed,
                    _champion.Stats.Value(StatType.AttackRange) + _rangeBuffer + 4f,
                    _projectileRadius, damage, DamageType.Physical, _unitMask,
                    false, _target, null, _projectileColor);
            }
            else
            {
                _target.TakeDamage(new DamageInfo(damage, DamageType.Physical, _champion,
                                                  isCrit, canLifesteal: true));
                // Son de coup pour les non-champions (les champions le jouent via CombatFeedback).
                if (!(_target is Champion))
                    Twisted3v3.Audio.Sfx.Play(Twisted3v3.Audio.SfxId.ImpactPhysical,
                        _target.Transform.position, 0.5f);
            }

            // Vol de vie sur l'attaque.
            float lifesteal = _champion.Stats.Value(StatType.Lifesteal);
            if (lifesteal > 0f) _champion.Heal(damage * lifesteal, _champion);

            OnAttack?.Invoke();
        }
    }
}
