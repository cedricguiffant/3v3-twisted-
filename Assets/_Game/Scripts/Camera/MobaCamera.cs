using UnityEngine;

namespace Twisted3v3.CameraControl
{
    /// <summary>
    /// Caméra MOBA top-down : suit une cible avec un offset fixe et un angle plongeant.
    /// Lissage du déplacement pour un rendu agréable. Brancher la cible via l'inspecteur
    /// ou <see cref="SetTarget"/> (ex: le champion du joueur au spawn).
    /// </summary>
    public sealed class MobaCamera : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [Tooltip("Décalage par rapport à la cible. Y haut + Z négatif = vue plongeante.")]
        [SerializeField] private Vector3 _offset = new(0f, 16f, -9f);
        [SerializeField] private float _followSmoothing = 10f;
        [SerializeField] private bool _lookAtTarget = true;

        public void SetTarget(Transform target) => _target = target;

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 desired = _target.position + _offset;
            transform.position = Vector3.Lerp(
                transform.position, desired, _followSmoothing * Time.deltaTime);

            if (_lookAtTarget) transform.LookAt(_target.position);
        }
    }
}
