using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Combat;
using Twisted3v3.Abilities;
using Twisted3v3.Minions;

namespace Twisted3v3.AI
{
    /// <summary>
    /// IA de champion. Priorités par tick : (1) se replier en base si les PV sont bas
    /// (hystérésis), sinon (2) engager le meilleur ennemi proche — champion choisi par
    /// score (PV bas + proximité), sinon sbire — sinon (3) pousser les structures pour
    /// faire avancer la partie. Délègue poursuite/attaque (et le kiting des rôles à
    /// distance) à l'<see cref="AutoAttack"/> ; garde l'ultime pour les champions.
    /// </summary>
    [RequireComponent(typeof(Champion))]
    public sealed class ChampionAI : MonoBehaviour
    {
        [SerializeField] private float _aggroRange = 18f;
        [SerializeField] private float _abilityCastRange = 7f;
        [SerializeField] private float _decisionInterval = 0.6f;
        [SerializeField] private LayerMask _unitMask = ~0;

        [Header("Repli")]
        [Tooltip("Sous ce ratio de PV, le bot se replie en base.")]
        [SerializeField, Range(0f, 1f)] private float _retreatHealth = 0.25f;
        [Tooltip("Il repart au combat une fois ce ratio de PV atteint (hystérésis).")]
        [SerializeField, Range(0f, 1f)] private float _resumeHealth = 0.55f;

        [Header("Ultime")]
        [Tooltip("L'ult n'est lancée sur un champion qu'en dessous de ce ratio de PV cible.")]
        [SerializeField, Range(0f, 1f)] private float _ultHealthThreshold = 0.6f;

        private static readonly Collider[] _buffer = new Collider[48];

        private Champion _champion;
        private AutoAttack _autoAttack;
        private AbilitySystem _abilities;
        private Progression.LevelSystem _levels;
        private float _timer;
        private bool _retreating;
        private Vector3 _basePosition;

        private void Start()
        {
            _champion = GetComponent<Champion>();
            _autoAttack = GetComponent<AutoAttack>();
            _abilities = _champion.Abilities;
            _levels = GetComponent<Progression.LevelSystem>();
            _basePosition = ResolveBasePosition();

            // Progression honnête : l'IA dépense ses points de compétence comme un
            // joueur (1 au spawn, +1 par niveau, ult verrouillée aux niveaux 6/11/16).
            if (_levels != null) _levels.OnLevelChanged += HandleLevelChanged;
            SpendSkillPoints();

            // Les rôles à distance kitent (orbe-walk) en duel.
            if (_autoAttack != null && _champion.Data != null)
                _autoAttack.Kite = _champion.Data.Role == ChampionRole.Marksman
                                 || _champion.Data.Role == ChampionRole.Mage;
        }

        private void OnDestroy()
        {
            if (_levels != null) _levels.OnLevelChanged -= HandleLevelChanged;
        }

        private void HandleLevelChanged(Progression.LevelSystem levels) => SpendSkillPoints();

        /// <summary>
        /// Dépense tous les points disponibles : R dès que possible, sinon la
        /// capacité de plus bas rang (priorité Q > Z > E à rang égal).
        /// </summary>
        private void SpendSkillPoints()
        {
            if (_levels == null)
            {
                _abilities.LevelUp(AbilitySlot.Q); // secours sans LevelSystem
                return;
            }

            for (int guard = 0; guard < 32 && _levels.SkillPoints > 0; guard++)
            {
                if (_levels.TryLevelAbility(AbilitySlot.R)) continue;

                AbilitySlot best = AbilitySlot.Q;
                int bestRank = int.MaxValue;
                foreach (var slot in new[] { AbilitySlot.Q, AbilitySlot.Z, AbilitySlot.E })
                {
                    var inst = _abilities.GetSlot(slot);
                    if (inst == null || inst.Rank >= inst.Data.MaxRank) continue;
                    if (inst.Rank < bestRank) { bestRank = inst.Rank; best = slot; }
                }
                if (bestRank == int.MaxValue || !_levels.TryLevelAbility(best)) break;
            }
        }

        /// <summary>Fontaine alliée (base) où se replier, sinon la position de spawn.</summary>
        private Vector3 ResolveBasePosition()
        {
            foreach (var f in Object.FindObjectsByType<FountainZone>(FindObjectsSortMode.None))
                if (f.Team == _champion.Team) return f.transform.position;
            return transform.position;
        }

        private void Update()
        {
            if (_champion.IsDead) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = _decisionInterval;

            // 0) Repli : bas PV → retour base jusqu'à récupération (hystérésis).
            if (ShouldRetreat())
            {
                _autoAttack?.ClearTarget();
                _champion.Motor.MoveTo(_basePosition);
                return;
            }

            // 1) Combat (meilleur champion par score, sinon sbire), sinon 2) push.
            IDamageable target = AcquireCombatTarget() ?? AcquirePushTarget();

            if (target == null)
            {
                _autoAttack?.ClearTarget();
                return;
            }

            _autoAttack?.SetTarget(target);

            if (Vector3.Distance(transform.position, target.Transform.position) <= _abilityCastRange)
                CastAbility(target);
        }

        /// <summary>Hystérésis de repli : déclenche sous _retreatHealth, s'arrête à _resumeHealth.</summary>
        private bool ShouldRetreat()
        {
            float hp = _champion.Health != null ? _champion.Health.HealthPercent : 1f;
            if (_retreating) _retreating = hp < _resumeHealth;
            else _retreating = hp < _retreatHealth;
            return _retreating;
        }

        /// <summary>
        /// Meilleur ennemi dans le rayon d'aggro. Les champions priment et sont notés
        /// (PV bas + proximité) ; à défaut, le sbire le plus proche.
        /// </summary>
        private IDamageable AcquireCombatTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _aggroRange, _buffer, _unitMask);
            Champion bestChamp = null;
            IDamageable nearestMinion = null;
            float bestScore = float.MaxValue, minionSq = float.MaxValue;
            Vector3 pos = transform.position;

            for (int i = 0; i < count; i++)
            {
                if (!_buffer[i].TryGetComponent<IDamageable>(out var d)) continue;
                if (d.IsDead || !_champion.IsEnemy(d)) continue;
                float sq = (d.Transform.position - pos).sqrMagnitude;

                if (d is Champion champ)
                {
                    // Score bas = meilleure cible : privilégie PV faibles et proximité.
                    float distNorm = Mathf.Sqrt(sq) / _aggroRange;
                    float hp = champ.Health != null ? champ.Health.HealthPercent : 1f;
                    float score = hp + distNorm * 0.5f;
                    if (score < bestScore) { bestScore = score; bestChamp = champ; }
                }
                else if (d is Minion && sq < minionSq) { minionSq = sq; nearestMinion = d; }
            }
            return (IDamageable)bestChamp ?? nearestMinion;
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

            // Ultime : réservée aux champions, et seulement si la cible est achevable
            // (sous le seuil de PV) — on ne la gaspille pas sur un sbire/plein PV.
            if (target is Champion tc && tc.Health != null
                && tc.Health.HealthPercent <= _ultHealthThreshold
                && _abilities.TryCast(AbilitySlot.R, aim, ground, target))
                return;

            foreach (var slot in new[] { AbilitySlot.E, AbilitySlot.Q, AbilitySlot.Z })
                if (_abilities.TryCast(slot, aim, ground, target)) return;
        }
    }
}
