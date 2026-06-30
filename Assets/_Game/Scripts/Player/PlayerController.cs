using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Combat;
using Twisted3v3.Progression;

namespace Twisted3v3.Player
{
    /// <summary>
    /// Traduit l'input du joueur en ordres au <see cref="Champion"/> : déplacement
    /// clic-droit et lancement des capacités Q/Z/E/R sous le curseur. Aucune logique
    /// de gameplay ici — il ne fait qu'appeler Motor et AbilitySystem (séparation
    /// des préoccupations). Remplaçable par une IA sans toucher au reste.
    ///
    /// Setup éditeur :
    ///   - Sol sur un layer "Ground" → renseigner GroundMask.
    ///   - Unités ciblables (colliders) sur un layer "Units" → renseigner UnitMask.
    /// </summary>
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private Champion _champion;
        [SerializeField] private Camera _camera;

        [Header("Raycast")]
        [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField] private LayerMask _unitMask = 0;
        [SerializeField] private float _maxRayDistance = 200f;

        [Header("Touches de capacités")]
        [SerializeField] private KeyCode _qKey = KeyCode.Q;
        [SerializeField] private KeyCode _zKey = KeyCode.Z;
        [SerializeField] private KeyCode _eKey = KeyCode.E;
        [SerializeField] private KeyCode _rKey = KeyCode.R;

        [Header("Attaque")]
        [Tooltip("Touche d'attaque-déplacement (style MOBA) : puis clic gauche pour viser.")]
        [SerializeField] private KeyCode _attackMoveKey = KeyCode.A;

        private AutoAttack _autoAttack;
        private LevelSystem _levels;
        private bool _attackMovePending;

        private void Awake()
        {
            if (_champion == null) _champion = GetComponent<Champion>();
            if (_camera == null) _camera = Camera.main;
            _autoAttack = _champion != null ? _champion.GetComponent<AutoAttack>() : null;
            _levels = _champion != null ? _champion.GetComponent<LevelSystem>() : null;
        }

        private void OnDisable() => CursorService.ShowAttack(false); // évite un curseur d'attaque collé (mort)

        private void Update()
        {
            if (_champion == null || _champion.IsDead) return;

            bool blockRightClick = HandleAttackMove();
            if (!blockRightClick) HandleRightClick();
            UpdateCursor();

            bool leveling = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (leveling)
            {
                // Ctrl + touche = investir un point de compétence (style MOBA).
                HandleAbilityLeveling(_qKey, AbilitySlot.Q);
                HandleAbilityLeveling(_zKey, AbilitySlot.Z);
                HandleAbilityLeveling(_eKey, AbilitySlot.E);
                HandleAbilityLeveling(_rKey, AbilitySlot.R);
            }
            else
            {
                HandleAbility(_qKey, AbilitySlot.Q);
                HandleAbility(_zKey, AbilitySlot.Z);
                HandleAbility(_eKey, AbilitySlot.E);
                HandleAbility(_rKey, AbilitySlot.R);
            }
        }

        /// <summary>
        /// Attaque-déplacement : "A" arme l'ordre (curseur d'attaque), puis le clic
        /// gauche le valide au point visé. Le clic droit / Échap l'annule.
        /// Renvoie true si l'input doit empêcher le traitement du clic droit ce frame.
        /// </summary>
        private bool HandleAttackMove()
        {
            if (Input.GetKeyDown(_attackMoveKey)) _attackMovePending = true;
            if (!_attackMovePending) return false;

            if (Input.GetKeyDown(KeyCode.Escape)) { _attackMovePending = false; return false; }
            if (Input.GetMouseButtonDown(1)) { _attackMovePending = false; return true; } // clic droit = annule

            if (Input.GetMouseButtonDown(0) && RaycastGround(out Vector3 point))
            {
                if (_autoAttack != null) _autoAttack.AttackMove(point);
                _attackMovePending = false;
            }
            return false;
        }

        /// <summary>Curseur d'attaque si attaque-déplacement armée OU survol d'un ennemi.</summary>
        private void UpdateCursor()
        {
            bool showAttack = _attackMovePending
                              || (RaycastUnit(out var unit) && _champion.IsEnemy(unit));
            CursorService.ShowAttack(showAttack);
        }

        private void HandleRightClick()
        {
            if (!Input.GetMouseButtonDown(1)) return;

            // Priorité à l'attaque : clic sur un ennemi → on l'attaque.
            if (RaycastUnit(out var unit) && _champion.IsEnemy(unit))
            {
                if (_autoAttack != null) _autoAttack.SetTarget(unit);
                return;
            }

            // Sinon, ordre de déplacement (et on abandonne la cible d'attaque).
            if (RaycastGround(out Vector3 point))
            {
                if (_autoAttack != null) _autoAttack.ClearTarget();
                _champion.Motor.MoveTo(point);
            }
        }

        private void HandleAbilityLeveling(KeyCode key, AbilitySlot slot)
        {
            if (Input.GetKeyDown(key) && _levels != null) _levels.TryLevelAbility(slot);
        }

        private void HandleAbility(KeyCode key, AbilitySlot slot)
        {
            if (!Input.GetKeyDown(key)) return;
            if (!RaycastGround(out Vector3 groundPoint)) return;

            // Direction visée = du champion vers le curseur, sur le plan horizontal.
            Vector3 aim = groundPoint - _champion.transform.position;
            aim.y = 0f;

            // Cible unité éventuelle sous le curseur (pour les sorts ciblés).
            IDamageable targetUnit = RaycastUnit(out var unit) ? unit : null;

            _champion.Abilities.TryCast(slot, aim, groundPoint, targetUnit);
        }

        private bool RaycastGround(out Vector3 point)
        {
            point = default;
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, _maxRayDistance, _groundMask))
            {
                point = hit.point;
                return true;
            }
            return false;
        }

        private bool RaycastUnit(out IDamageable unit)
        {
            unit = null;
            if (_unitMask.value == 0) return false;
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, _maxRayDistance, _unitMask)
                && hit.collider.TryGetComponent(out unit))
                return true;
            return false;
        }
    }
}
