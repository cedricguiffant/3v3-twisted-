using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Abilities;
using Twisted3v3.Progression;

namespace Twisted3v3.UI
{
    /// <summary>
    /// HUD de la barre de sorts (Q/Z/E/R) : icône, overlay de cooldown radial,
    /// pips de rang, et un bouton « + » par sort pour investir un point de
    /// compétence à la souris (sans raccourci clavier). Auto-construit son Canvas :
    /// il suffit d'ajouter ce composant et de référencer le champion.
    /// </summary>
    public sealed class AbilityBarUI : MonoBehaviour
    {
        [SerializeField] private Champion _champion;
        [SerializeField] private Vector2 _slotSize = new(96f, 96f);
        [SerializeField] private float _gap = 10f;
        [SerializeField] private float _bottomMargin = 28f;

        private LevelSystem _levels;
        private AbilitySystem _abilities;
        private Sprite _whiteSprite;
        private Font _font;
        private Text _pointsLabel;

        // Couleurs de secours par slot quand l'AbilityData n'a pas d'icône.
        private static readonly Color[] SlotTints =
        {
            new(0.55f, 0.25f, 0.65f), // Q
            new(0.20f, 0.45f, 0.70f), // Z
            new(0.65f, 0.45f, 0.20f), // E
            new(0.70f, 0.20f, 0.25f), // R
        };

        private sealed class SlotView
        {
            public AbilitySlot Slot;
            public Image Icon;
            public Image CooldownFill;
            public Text CooldownText;
            public Image[] Pips;
            public GameObject LevelButton;
            public CanvasGroup Group;
        }

        private readonly List<SlotView> _slots = new();

        private void Awake()
        {
            if (_champion == null) _champion = Object.FindFirstObjectByType<Champion>();
        }

        private void Start()
        {
            if (_champion == null) { Debug.LogWarning("[AbilityBarUI] Aucun Champion référencé."); return; }
            _levels = _champion.GetComponent<LevelSystem>();
            _abilities = _champion.Abilities;

            _whiteSprite = BuildWhiteSprite();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            EnsureEventSystem();
            BuildUI();
        }

        // ----------------------------------------------------------------- BUILD
        private void BuildUI()
        {
            var canvas = BuildCanvas();

            var slots = new[] { AbilitySlot.Q, AbilitySlot.Z, AbilitySlot.E, AbilitySlot.R };
            var keys = new[] { "Q", "Z", "E", "R" };

            float barWidth = slots.Length * _slotSize.x + (slots.Length - 1) * _gap;

            var panel = NewRect("AbilityBar", canvas.transform);
            SetRect(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, _bottomMargin), new Vector2(barWidth, _slotSize.y + 22f));

            float startX = -(barWidth - _slotSize.x) * 0.5f;
            for (int i = 0; i < slots.Length; i++)
            {
                float x = startX + i * (_slotSize.x + _gap);
                _slots.Add(BuildSlot(panel, slots[i], keys[i], SlotTints[i], new Vector2(x, 14f)));
            }

            // Compteur de points de compétence au-dessus de la barre.
            var pts = NewRect("SkillPoints", panel);
            SetRect(pts, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 22f), new Vector2(260f, 26f));
            _pointsLabel = AddText(pts.gameObject, "", 16, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.4f));
        }

        private SlotView BuildSlot(RectTransform parent, AbilitySlot slot, string key, Color tint, Vector2 pos)
        {
            var view = new SlotView { Slot = slot };

            var root = NewRect("Slot_" + key, parent);
            SetRect(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    pos, _slotSize);
            view.Group = root.gameObject.AddComponent<CanvasGroup>();

            // Fond
            AddImage(root.gameObject, new Color(0.08f, 0.08f, 0.10f, 0.95f), _whiteSprite);

            // Icône (sprite de l'AbilityData si dispo, sinon couleur de slot)
            var iconRect = NewRect("Icon", root);
            SetRect(iconRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f));
            view.Icon = AddImage(iconRect.gameObject, tint, _whiteSprite);

            // Lettre de la touche
            var keyRect = NewRect("Key", root);
            SetRect(keyRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -2f), new Vector2(0f, 26f));
            AddText(keyRect.gameObject, key, 18, TextAnchor.UpperCenter, Color.white);

            // Overlay de cooldown (radial)
            var cdRect = NewRect("Cooldown", root);
            SetRect(cdRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f));
            view.CooldownFill = AddImage(cdRect.gameObject, new Color(0f, 0f, 0f, 0.65f), _whiteSprite);
            view.CooldownFill.type = Image.Type.Filled;
            view.CooldownFill.fillMethod = Image.FillMethod.Radial360;
            view.CooldownFill.fillOrigin = (int)Image.Origin360.Top;
            view.CooldownFill.fillClockwise = false;
            view.CooldownFill.raycastTarget = false;

            // Texte du cooldown restant
            view.CooldownText = AddText(root.gameObject, "", 22, TextAnchor.MiddleCenter, Color.white);

            // Pips de rang (sous le slot)
            view.Pips = BuildPips(root, slot);

            // Bouton « + » (haut-droite)
            view.LevelButton = BuildLevelButton(root, slot);

            return view;
        }

        private Image[] BuildPips(RectTransform slotRoot, AbilitySlot slot)
        {
            var inst = _abilities != null ? _abilities.GetSlot(slot) : null;
            int max = inst != null ? Mathf.Max(1, inst.Data.MaxRank) : 5;

            var pips = new Image[max];
            float pipGap = 3f;
            float pipW = (_slotSize.x - (max - 1) * pipGap) / max;
            for (int i = 0; i < max; i++)
            {
                var pip = NewRect("Pip" + i, slotRoot);
                float px = -_slotSize.x * 0.5f + pipW * 0.5f + i * (pipW + pipGap);
                SetRect(pip, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                        new Vector2(px, -3f), new Vector2(pipW, 6f));
                pips[i] = AddImage(pip.gameObject, Color.gray, _whiteSprite);
            }
            return pips;
        }

        private GameObject BuildLevelButton(RectTransform slotRoot, AbilitySlot slot)
        {
            var btnRect = NewRect("LevelUp", slotRoot);
            SetRect(btnRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(4f, 4f), new Vector2(30f, 30f));
            AddImage(btnRect.gameObject, new Color(0.15f, 0.65f, 0.2f, 1f), _whiteSprite);
            AddText(btnRect.gameObject, "+", 24, TextAnchor.MiddleCenter, Color.white);

            var button = btnRect.gameObject.AddComponent<Button>();
            AbilitySlot captured = slot; // capture pour la closure
            button.onClick.AddListener(() =>
            {
                if (_levels != null) _levels.TryLevelAbility(captured);
            });

            btnRect.gameObject.SetActive(false);
            return btnRect.gameObject;
        }

        // ---------------------------------------------------------------- UPDATE
        private void Update()
        {
            if (_abilities == null) return;

            foreach (var view in _slots)
            {
                var inst = _abilities.GetSlot(view.Slot);
                bool learned = inst != null && inst.IsLearned;

                view.Group.alpha = learned ? 1f : 0.45f;

                // Cooldown
                if (inst != null && learned && !inst.IsReady)
                {
                    view.CooldownFill.fillAmount = inst.CooldownPercent;
                    view.CooldownText.text = inst.CooldownRemaining.ToString("0.0");
                }
                else
                {
                    view.CooldownFill.fillAmount = 0f;
                    view.CooldownText.text = "";
                }

                // Pips de rang
                int rank = inst != null ? inst.Rank : 0;
                for (int i = 0; i < view.Pips.Length; i++)
                    view.Pips[i].color = i < rank
                        ? new Color(1f, 0.85f, 0.3f)
                        : new Color(0.3f, 0.3f, 0.3f);

                // Bouton « + »
                view.LevelButton.SetActive(CanLevel(inst, view.Slot));
            }

            if (_pointsLabel != null && _levels != null)
                _pointsLabel.text = _levels.SkillPoints > 0
                    ? $"Points de compétence : {_levels.SkillPoints}"
                    : "";
        }

        private bool CanLevel(AbilityInstance inst, AbilitySlot slot)
        {
            if (_levels == null || _levels.SkillPoints <= 0) return false;
            if (inst == null || inst.Rank >= inst.Data.MaxRank) return false;

            if (slot == AbilitySlot.R)
            {
                int required = inst.Rank == 0 ? 6 : inst.Rank == 1 ? 11 : 16;
                if (_champion.Level < required) return false;
            }
            return true;
        }

        // -------------------------------------------------------------- HELPERS
        private Canvas BuildCanvas()
        {
            var go = new GameObject("HUD_Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es.hideFlags = HideFlags.None;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        private static Image AddImage(GameObject go, Color color, Sprite sprite)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            return img;
        }

        private Text AddText(GameObject go, string content, int size, TextAnchor anchor, Color color)
        {
            var t = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            t.SetParent(go.transform, false);
            SetRect(t, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var text = t.gameObject.AddComponent<Text>();
            text.text = content;
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Sprite BuildWhiteSprite()
        {
            var tex = new Texture2D(4, 4);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
