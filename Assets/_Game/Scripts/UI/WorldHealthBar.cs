using UnityEngine;
using UnityEngine.UI;
using Twisted3v3.Combat;

namespace Twisted3v3.UI
{
    /// <summary>
    /// Barre de vie flottante world-space au-dessus d'une unité. Lit l'état via
    /// <see cref="IHealthInfo"/> (sur le même GameObject), s'oriente vers la caméra,
    /// se masque à la mort. Auto-construite : poser le composant suffit.
    /// </summary>
    public sealed class WorldHealthBar : MonoBehaviour
    {
        [SerializeField] private Vector3 _offset = new(0f, 2.4f, 0f);
        [SerializeField] private Vector2 _pixelSize = new(140f, 18f);
        [SerializeField] private Color _fillColor = new(0.3f, 0.85f, 0.3f);
        [SerializeField] private float _worldScale = 0.01f;

        private IHealthInfo _health;
        private Camera _camera;
        private Transform _root;
        private Image _fill;

        /// <summary>Permet de personnaliser la couleur avant Start (ex: monstres).</summary>
        public void SetColor(Color color) => _fillColor = color;

        /// <summary>Hauteur de la barre au-dessus de l'unité (grands bâtiments).</summary>
        public void SetOffsetY(float y) => _offset = new Vector3(0f, y, 0f);

        /// <summary>Largeur de la barre (bâtiments plus larges).</summary>
        public void SetWidth(float width) => _pixelSize = new Vector2(width, _pixelSize.y);

        private bool _concealed;

        /// <summary>Masque/affiche la barre (dissimulation par la vision / les buissons).</summary>
        public void SetVisible(bool visible) => _concealed = !visible;

        private void Start()
        {
            _health = GetComponent<IHealthInfo>();
            _camera = Camera.main;
            if (_health == null) { enabled = false; return; }
            Build();
        }

        private void Build()
        {
            var sprite = MakeWhiteSprite();

            var go = new GameObject("HealthBar", typeof(Canvas));
            _root = go.transform;
            _root.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)_root;
            rt.sizeDelta = _pixelSize;
            _root.localScale = Vector3.one * _worldScale;

            // Fond
            var bg = NewChild("BG", rt, sprite, new Color(0f, 0f, 0f, 0.7f));
            Stretch(bg);

            // Remplissage (Image Filled horizontale)
            var fillRect = NewChild("Fill", rt, sprite, _fillColor);
            Stretch(fillRect, 2f);
            _fill = fillRect.GetComponent<Image>();
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fill.fillAmount = 1f;
        }

        private void LateUpdate()
        {
            if (_health == null || _root == null) return;

            bool hidden = _health.IsDead || _concealed;
            if (_root.gameObject.activeSelf == hidden) _root.gameObject.SetActive(!hidden);
            if (hidden) return;

            _root.position = transform.position + _offset;
            if (_camera != null) _root.rotation = _camera.transform.rotation; // billboard

            float pct = _health.MaxHealth > 0f
                ? Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth) : 0f;
            _fill.fillAmount = pct;
            _fill.color = Color.Lerp(new Color(0.8f, 0.2f, 0.2f), _fillColor, pct);
        }

        // ---- helpers ----
        private static RectTransform NewChild(string name, RectTransform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite; img.color = color; img.raycastTarget = false;
            return rt;
        }

        private static void Stretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        private static Sprite MakeWhiteSprite()
        {
            var tex = new Texture2D(4, 4);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
