using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Minions;

namespace Twisted3v3.AI
{
    /// <summary>
    /// IA de champion : engage l'ennemi proche (champion en priorité, puis sbire) ;
    /// à défaut, part pousser les structures ennemies (tours, puis Nexus) pour faire
    /// avancer la partie. Délègue la poursuite/attaque à l'AutoAttack et lance ses
    /// capacités à portée. Apprend ses sorts au spawn.
    /// </summary>
    [RequireComponent(typeof(Champion))]
    public sealed class ChampionAI : MonoBehaviour
    {
        [SerializeField] private float _aggroRange = 18f;
        [SerializeField] private float _abilityCastRange = 7f;
        [SerializeField] private float _decisionInterval = 0.6f;
        [SerializeField, Range(1, 5)] private int _abilityRank = 3;
        [SerializeField] private LayerMask _unitMask = ~0;

        private static readonly Collider[] _buffer = new Collider[48];

        private Champion _champion;
        private AutoAttack _autoAttack;
        private AbilitySystem _abilities;
        private float _timer;

        private void Start()
        {
            _champion = GetComponent<Champion>();
            _autoAttack = GetComponent<AutoAttack>();
            _abilities = _champion.Abilities;
            LearnAbilities();
        }

        private void LearnAbilities()
        {
            foreach (var slot in new[] { AbilitySlot.Q, AbilitySlot.Z, AbilitySlot.E })
                for (int i = 0; i < _abilityRank; i++) _abilities.LevelUp(slot);
            _abilities.LevelUp(AbilitySlot.R);
        }

        private void Update()
        {
            if (_champion.IsDead) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = _decisionInterval;

            // 1) Combat rapproché (champion > sbire), sinon 2) pousser les structures.
            IDamageable target = AcquireCombatTarget() ?? AcquirePushTarget();

            if (target == null)
            {
                if (_autoAttack != null) _autoAttack.ClearTarget();
                return;
            }

            if (_autoAttack != null) _autoAttack.SetTarget(target);

            // Capacités si la cible est à portée de sort.
            if (Vector3.Distance(transform.position, target.Transform.position) <= _abilityCastRange)
                CastAbility(target);
        }

        /// <summary>Ennemi le plus proche dans le rayon d'aggro : champion prioritaire, puis sbire.</summary>
        private IDamageable AcquireCombatTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _aggroRange, _buffer, _unitMask);
            IDamageable champ = null, minion = null;
            float champSq = float.MaxValue, minionSq = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (!_buffer[i].TryGetComponent<IDamageable>(out var d)) continue;
                if (d.IsDead || !_champion.IsEnemy(d)) continue;
                float sq = (d.Transform.position - transform.position).sqrMagnitude;

                if (d is Champion && sq < champSq) { champSq = sq; champ = d; }
                else if (d is Minion && sq < minionSq) { minionSq = sq; minion = d; }
            }
            return champ ?? minion;
        }

        /// <summary>Structure ennemie attaquable la plus proche (tour avant Nexus invulnérable).</summary>
        private IDamageable AcquirePushTarget()
        {
            Structure best = null;
            float bestSq = float.MaxValue;
            foreach (var s in Object.FindObjectsByType<Structure>(FindObjectsSortMode.None))
            {
                if (s.IsDead || s.IsInvulnerable || !_champion.IsEnemy(s)) continue;
                float sq = (s.transform.position - transform.position).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = s; }
            }
            return best;
        }

        private void CastAbility(IDamageable target)
        {
            Vector3 aim = target.Transform.position - transform.position; aim.y = 0f;
            Vector3 ground = target.Transform.position;
            foreach (var slot in new[] { AbilitySlot.R, AbilitySlot.E, AbilitySlot.Q, AbilitySlot.Z })
                if (_abilities.TryCast(slot, aim, ground, target)) return;
        }
    }
}
