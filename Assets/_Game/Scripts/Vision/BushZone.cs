using System.Collections.Generic;
using UnityEngine;

namespace Twisted3v3.Vision
{
    /// <summary>
    /// Buisson : zone qui dissimule les unités à l'intérieur (façon Twisted Treeline).
    /// Un ennemi dans un buisson est invisible pour le joueur tant que son équipe n'a
    /// pas de vision dedans. Se contente de définir la zone ; la logique de visibilité
    /// vit dans <see cref="VisionSystem"/>. S'auto-enregistre et bootstrape le système.
    /// </summary>
    public sealed class BushZone : MonoBehaviour
    {
        [SerializeField] private float _radius = 4f;
        [Tooltip("Affiche un disque vert translucide au sol pour matérialiser le buisson.")]
        [SerializeField] private bool _showMarker = true;

        private static readonly List<BushZone> _all = new();
        public static IReadOnlyList<BushZone> All => _all;

        public float Radius => _radius;

        /// <summary>Vrai si le point (au sol) est dans le buisson.</summary>
        public bool Contains(Vector3 worldPos)
        {
            Vector3 d = worldPos - transform.position; d.y = 0f;
            return d.sqrMagnitude <= _radius * _radius;
        }

        private void OnEnable()
        {
            if (!_all.Contains(this)) _all.Add(this);
            VisionSystem.EnsureExists();
            if (_showMarker && Application.isPlaying) BuildMarker();
        }

        private void OnDisable() => _all.Remove(this);

        private void BuildMarker()
        {
            // Disque discret (lisibilité gameplay de la zone)...
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "BushMarker";
            marker.transform.SetParent(transform, false);
            marker.transform.localScale = new Vector3(_radius * 2f, 0.03f, _radius * 2f);
            marker.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            if (marker.TryGetComponent<Collider>(out var col)) Destroy(col);
            var rend = marker.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.12f, 0.38f, 0.16f, 0.3f) };
            rend.material = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // ...et de vraies HAUTES HERBES (touffes procédurales, déterministes par buisson).
            var rng = new System.Random(Mathf.RoundToInt(transform.position.x * 73f + transform.position.z * 131f));
            int tufts = Mathf.RoundToInt(_radius * _radius * 1.4f);
            for (int i = 0; i < tufts; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float dist = Mathf.Sqrt((float)rng.NextDouble()) * _radius * 0.9f;
                Vector3 pos = transform.position
                              + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                pos.y = 0f;

                float height = 1.0f + (float)rng.NextDouble() * 0.6f; // herbe haute (cache un champion)
                float g = 0.8f + (float)rng.NextDouble() * 0.35f;
                Twisted3v3.VFX.Grass.CreateTuft(pos, height, new Color(g * 0.9f, g, g * 0.85f),
                    transform, (float)rng.NextDouble());
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.7f, 0.3f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
