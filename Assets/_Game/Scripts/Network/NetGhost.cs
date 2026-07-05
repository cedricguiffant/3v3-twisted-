using UnityEngine;
using Twisted3v3.Champions;

namespace Twisted3v3.Net
{
    /// <summary>
    /// Vue client d'un champion piloté par le serveur : interpole la position/
    /// rotation vers le dernier snapshot (téléporte si trop loin) et applique
    /// les visuels de mort/réapparition. Sur le champion local, seule la partie
    /// mort/visuels est utilisée (la position est prédite localement).
    /// </summary>
    [RequireComponent(typeof(Champion))]
    public sealed class NetGhost : MonoBehaviour
    {
        private const float LerpSpeed = 12f;
        private const float TeleportDistance = 8f;

        /// <summary>Champion local (prédit) : pas d'interpolation de position.</summary>
        public bool IsLocal;

        private Champion _champion;
        private ChampionVisuals _visuals;
        private Collider[] _colliders;
        private Vector3 _targetPos;
        private float _targetRotY;
        private bool _hasTarget;
        private bool _dead;

        private void Awake()
        {
            _champion = GetComponent<Champion>();
            _visuals = ChampionVisuals.Of(_champion);
        }

        public void SetTarget(Vector3 position, float rotY)
        {
            _targetPos = position;
            _targetRotY = rotY;
            _hasTarget = true;
        }

        public void SetDead(bool dead)
        {
            if (dead == _dead) return;
            _dead = dead;

            _colliders = GetComponentsInChildren<Collider>(true);
            if (_visuals != null) _visuals.SetVisible(!dead);
            foreach (var c in _colliders)
                if (c != null) c.enabled = !dead;
        }

        private void Update()
        {
            if (IsLocal || !_hasTarget || _dead) return;

            Vector3 current = transform.position;
            if ((current - _targetPos).sqrMagnitude > TeleportDistance * TeleportDistance)
                transform.position = _targetPos; // gros écart (respawn, dash) : on saute
            else
                transform.position = Vector3.Lerp(current, _targetPos, LerpSpeed * Time.deltaTime);

            var targetRot = Quaternion.Euler(0f, _targetRotY, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, LerpSpeed * Time.deltaTime);
        }
    }
}
