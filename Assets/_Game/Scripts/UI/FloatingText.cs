using UnityEngine;

namespace Twisted3v3.UI
{
    /// <summary>
    /// Texte flottant world-space (dégâts, soins, « CRIT »). Auto-animé : monte,
    /// s'estompe, fait face à la caméra, puis se détruit. Créé via <see cref="Spawn"/>.
    /// Utilise un <c>TextMesh</c> (pas de Canvas) → zéro configuration, léger.
    /// </summary>
    public sealed class FloatingText : MonoBehaviour
    {
        private static Font _font;

        private TextMesh _mesh;
        private Camera _camera;
        private float _life;
        private float _maxLife;
        private Vector3 _velocity;
        private Color _color;

        /// <summary>Fait apparaître un texte flottant à une position monde.</summary>
        public static void Spawn(Vector3 worldPos, string text, Color color,
            float fontScale = 1f, float life = 0.8f)
        {
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            var go = new GameObject("FloatingText");
            go.transform.position = worldPos + Vector3.up * 2.2f
                                    + new Vector3(Random.Range(-0.4f, 0.4f), 0f, 0f);

            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = _font;
            tm.fontSize = 48;
            tm.characterSize = 0.14f * fontScale;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            go.GetComponent<MeshRenderer>().material = _font.material;

            var ft = go.AddComponent<FloatingText>();
            ft._mesh = tm;
            ft._color = color;
            ft._camera = Camera.main;
            ft._maxLife = ft._life = life;
            ft._velocity = new Vector3(0f, 2.4f, 0f);
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }

            transform.position += _velocity * Time.deltaTime;
            _velocity.y -= 3.5f * Time.deltaTime; // léger ralentissement vers le haut

            // Fondu sur la seconde moitié de vie.
            float t = _life / _maxLife;
            var c = _color; c.a = Mathf.Clamp01(t * 2f);
            _mesh.color = c;

            // Billboard caméra.
            if (_camera == null) _camera = Camera.main;
            if (_camera != null)
                transform.rotation = Quaternion.LookRotation(transform.position - _camera.transform.position);
        }
    }
}
