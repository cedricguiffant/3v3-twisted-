using UnityEngine;
using UnityEngine.UI;
using Twisted3v3.Champions;
using Twisted3v3.Progression;
using Twisted3v3.Economy;

namespace Twisted3v3.UI
{
    /// <summary>
    /// Panneau HUD du joueur (bas-gauche) : nom, niveau, barres PV / Mana / XP et or.
    /// Auto-construit son Canvas. Remplace l'overlay debug OnGUI du test harness.
    /// </summary>
    public sealed class HUDController : MonoBehaviour
    {
        [SerializeField] private Champion _champion;

        private LevelSystem _levels;
        private GoldWallet _wallet;
        private Font _font;
        private Sprite _white;

        private Image _hpFill, _manaFill, _xpFill;
        private Text _nameText, _hpText, _manaText, _levelText, _goldText;

        private void Start()
        {
            if (_champion == null) _champion = Object.FindFirstObjectByType<Champion>();
            if (_champion == null) { enabled = false; return; }
            _levels = _champion.GetComponent<LevelSystem>();
            _wallet = _champion.GetComponent<GoldWallet>();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _white = MakeWhite();
            Build();
        }

        private void Build()
        {
            var go = new GameObject("HUD_StatusCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            // Panneau bas-gauche
            var panel = NewRect("StatusPanel", canvas.transform);
            SetRect(panel, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                    new Vector2(20f, 20f), new Vector2(380f, 150f));
            AddImage(panel.gameObject, new Color(0.05f, 0.06f, 0.09f, 0.85f));

            _nameText = AddText(panel, "Champion", 20, new Vector2(12f, -10f), new Vector2(260f, 26f),
                                TextAnchor.UpperLeft, new Color(0.9f, 0.85f, 0.5f));

            // Badge niveau (rond) en haut-gauche
            var badge = NewRect("LevelBadge", panel);
            SetRect(badge, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-10f, -8f), new Vector2(44f, 44f));
            AddImage(badge.gameObject, new Color(0.15f, 0.18f, 0.28f, 1f));
            _levelText = AddText(badge, "1", 22, Vector2.zero, new Vector2(44f, 44f),
                                 TextAnchor.MiddleCenter, Color.white);

            // Barre PV
            _hpFill = AddBar(panel, new Vector2(12f, -44f), new Color(0.3f, 0.8f, 0.3f), out _hpText);
            // Barre Mana
            _manaFill = AddBar(panel, new Vector2(12f, -74f), new Color(0.3f, 0.5f, 0.95f), out _manaText);

            // Barre XP (fine)
            var xpBg = NewRect("XPBar", panel);
            SetRect(xpBg, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 22f), new Vector2(-24f, 8f));
            AddImage(xpBg.gameObject, new Color(0.1f, 0.1f, 0.12f, 1f));
            var xpFillRect = NewRect("Fill", xpBg);
            Stretch(xpFillRect);
            _xpFill = AddImage(xpFillRect.gameObject, new Color(0.8f, 0.7f, 0.25f));
            _xpFill.type = Image.Type.Filled; _xpFill.fillMethod = Image.FillMethod.Horizontal;

            // Or
            _goldText = AddText(panel, "0", 18, new Vector2(12f, 28f), new Vector2(200f, 22f),
                                TextAnchor.LowerLeft, new Color(1f, 0.85f, 0.3f));
        }

        private void Update()
        {
            if (_champion == null || _champion.Health == null) return;

            _nameText.text = _champion.Data ? _champion.Data.DisplayName : "Champion";
            _levelText.text = _champion.Level.ToString();

            float hp = _champion.Health.CurrentHealth, hpMax = _champion.Health.MaxHealth;
            _hpFill.fillAmount = hpMax > 0f ? hp / hpMax : 0f;
            _hpText.text = $"{hp:0} / {hpMax:0}" +
                           (_champion.Health.Shield > 0f ? $"  (+{_champion.Health.Shield:0})" : "");

            float manaMax = _champion.Stats != null ? _champion.Stats.Value(Stats.StatType.MaxMana) : 0f;
            _manaFill.fillAmount = manaMax > 0f ? _champion.CurrentMana / manaMax : 0f;
            _manaText.text = $"{_champion.CurrentMana:0} / {manaMax:0}";

            if (_levels != null)
                _xpFill.fillAmount = _levels.XpToNextLevel > 0f
                    ? Mathf.Clamp01(_levels.CurrentXp / _levels.XpToNextLevel) : 0f;
            if (_wallet != null) _goldText.text = "Or : " + _wallet.Gold;
        }

        // ---- helpers ----
        private Image AddBar(RectTransform panel, Vector2 pos, Color color, out Text label)
        {
            var bg = NewRect("Bar", panel);
            SetRect(bg, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                    pos, new Vector2(-24f, 24f));
            // largeur = parent - 24 via offset : on étire en largeur
            bg.anchorMin = new Vector2(0f, 1f); bg.anchorMax = new Vector2(1f, 1f);
            bg.offsetMin = new Vector2(12f, pos.y - 24f); bg.offsetMax = new Vector2(-12f, pos.y);
            AddImage(bg.gameObject, new Color(0.1f, 0.1f, 0.12f, 1f));

            var fillRect = NewRect("Fill", bg);
            Stretch(fillRect);
            var fill = AddImage(fillRect.gameObject, color);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;

            label = AddText(bg, "", 14, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, Color.white);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            return fill;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot,
            Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private Image AddImage(GameObject go, Color color)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.sprite = _white; img.color = color; img.raycastTarget = false;
            return img;
        }

        private Text AddText(RectTransform parent, string content, int size, Vector2 pos,
            Vector2 sizeDelta, TextAnchor anchor, Color color)
        {
            var rt = NewRect("Text", parent);
            SetRect(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), pos, sizeDelta);
            var text = rt.gameObject.AddComponent<Text>();
            text.text = content; text.font = _font; text.fontSize = size; text.fontStyle = FontStyle.Bold;
            text.alignment = anchor; text.color = color; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        private static Sprite MakeWhite()
        {
            var tex = new Texture2D(4, 4);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
