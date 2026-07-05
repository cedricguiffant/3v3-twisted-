using System.Collections.Generic;
using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Combat;
using Twisted3v3.Player;

namespace Twisted3v3.Net
{
    /// <summary>
    /// Version réseau du <see cref="PlayerController"/> : mêmes contrôles
    /// (clic droit, attaque-déplacement, Q/Z/E/R, Ctrl+touche pour monter un
    /// sort), mais chaque ordre est à la fois *prédit localement* (réactivité)
    /// et *envoyé au serveur* (autorité). La montée de sort passe uniquement
    /// par le serveur, qui renvoie le rang validé.
    /// </summary>
    [RequireComponent(typeof(Champion))]
    public sealed class NetClientController : MonoBehaviour
    {
        private Champion _champion;
        private Camera _camera;
        private AutoAttack _autoAttack;
        private RecallController _recall;
        private NetClient _net;

        private LayerMask _groundMask = ~0;
        private LayerMask _unitMask = 0;
        private float _maxRayDistance = 200f;
        // Défauts AZERTY : sorts sur A Z E R, attaque-déplacement sur Q.
        private KeyCode _qKey = KeyCode.A;
        private KeyCode _zKey = KeyCode.Z;
        private KeyCode _eKey = KeyCode.E;
        private KeyCode _rKey = KeyCode.R;
        private KeyCode _attackMoveKey = KeyCode.Q;
        private KeyCode _backKey = KeyCode.B;

        private bool _attackMovePending;

        /// <summary>Branche le client réseau et reprend les réglages du PlayerController de la scène.</summary>
        public void Configure(NetClient net, Dictionary<string, object> settings)
        {
            _net = net;
            if (settings == null)
            {
                _groundMask = LayerMask.GetMask("Ground");
                _unitMask = LayerMask.GetMask("Units");
                return;
            }
            if (settings.TryGetValue("_groundMask", out var g)) _groundMask = (LayerMask)g;
            if (settings.TryGetValue("_unitMask", out var u)) _unitMask = (LayerMask)u;
            if (settings.TryGetValue("_maxRayDistance", out var d)) _maxRayDistance = (float)d;
            if (settings.TryGetValue("_qKey", out var q)) _qKey = (KeyCode)q;
            if (settings.TryGetValue("_zKey", out var z)) _zKey = (KeyCode)z;
            if (settings.TryGetValue("_eKey", out var e)) _eKey = (KeyCode)e;
            if (settings.TryGetValue("_rKey", out var r)) _rKey = (KeyCode)r;
            if (settings.TryGetValue("_attackMoveKey", out var a)) _attackMoveKey = (KeyCode)a;
        }

        private void Awake()
        {
            _champion = GetComponent<Champion>();
            _camera = Camera.main;
            _autoAttack = GetComponent<AutoAttack>();
            _recall = GetComponent<RecallController>() ?? gameObject.AddComponent<RecallController>();
        }

        private void OnDisable() => CursorService.ShowAttack(false);

        private void Update()
        {
            if (_champion == null || _champion.IsDead || _net == null) return;
            if (_camera == null) { _camera = Camera.main; if (_camera == null) return; }

            if (_recall != null && _recall.IsChanneling)
            {
                if (Input.GetKeyDown(_backKey)) { _recall.Cancel(); _net.SendRecall(); }
                else if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)
                         || Input.GetKeyDown(_attackMoveKey) || Input.GetKeyDown(_qKey)
                         || Input.GetKeyDown(_zKey) || Input.GetKeyDown(_eKey) || Input.GetKeyDown(_rKey))
                {
                    _recall.Cancel();
                    _net.SendRecall();
                }
                return;
            }
            if (Input.GetKeyDown(_backKey)) { _recall?.Toggle(); _net.SendRecall(); return; }

            bool blockRightClick = HandleAttackMove();
            if (!blockRightClick) HandleRightClick();
            UpdateCursor();

            bool leveling = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (leveling)
            {
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

        private bool HandleAttackMove()
        {
            if (Input.GetKeyDown(_attackMoveKey)) _attackMovePending = true;
            if (!_attackMovePending) return false;

            if (Input.GetKeyDown(KeyCode.Escape)) { _attackMovePending = false; return false; }
            if (Input.GetMouseButtonDown(1)) { _attackMovePending = false; return true; }

            if (Input.GetMouseButtonDown(0) && RaycastGround(out Vector3 point))
            {
                _autoAttack?.AttackMove(point); // prédiction
                _net.SendAttackMove(point);
                _attackMovePending = false;
            }
            return false;
        }

        private void UpdateCursor()
        {
            bool showAttack = _attackMovePending
                              || (RaycastUnit(out var unit) && _champion.IsEnemy(unit));
            CursorService.ShowAttack(showAttack);
        }

        private void HandleRightClick()
        {
            if (!Input.GetMouseButtonDown(1)) return;

            if (RaycastUnit(out var unit) && _champion.IsEnemy(unit))
            {
                _autoAttack?.SetTarget(unit); // prédiction
                _net.SendAttackTarget(unit);
                return;
            }

            if (RaycastGround(out Vector3 point))
            {
                _autoAttack?.ClearTarget();
                _champion.Motor.MoveTo(point); // prédiction
                _net.SendMove(point);
            }
        }

        private void HandleAbilityLeveling(KeyCode key, AbilitySlot slot)
        {
            // Autorité serveur : le rang validé revient via EvtAbilityLeveled.
            if (Input.GetKeyDown(key)) _net.SendLevelAbility(slot);
        }

        private void HandleAbility(KeyCode key, AbilitySlot slot)
        {
            if (!Input.GetKeyDown(key)) return;
            if (!RaycastGround(out Vector3 groundPoint)) return;

            Vector3 aim = groundPoint - _champion.transform.position;
            aim.y = 0f;
            IDamageable targetUnit = RaycastUnit(out var unit) ? unit : null;

            // Cast prédit localement (visuel immédiat), validé par le serveur.
            _champion.Abilities.TryCast(slot, aim, groundPoint, targetUnit);
            _net.SendCast(slot, aim, groundPoint, targetUnit);
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
