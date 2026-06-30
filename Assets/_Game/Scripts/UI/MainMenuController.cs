using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Twisted3v3.UI
{
    /// <summary>
    /// Menu principal minimal : titre + boutons JOUER / QUITTER. Auto-construit son
    /// Canvas et son EventSystem (aucun assemblage manuel). La musique du menu est
    /// jouée par un MusicPlayer séparé dans la scène.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string _gameScene = "Map_3v3";
        [SerializeField] private string _title = "TWISTED 3v3";
        [Tooltip("Splash du champion affiché à droite du menu.")]
        [SerializeField] private Sprite _splashPortrait;

        private Font _font;

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            EnsureEventSystem();
            Build();
        }

        private void Build()
        {
            var go = new GameObject("Menu_Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            // Fond sombre
            var bg = NewRect("Background", canvas.transform);
            SetStretch(bg);
            AddImage(bg.gameObject, new Color(0.04f, 0.05f, 0.08f, 1f));

            // Titre
            var title = NewRect("Title", canvas.transform);
            SetRect(title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 180f), new Vector2(900f, 140f));
            AddText(title.gameObject, _title, 84, TextAnchor.MiddleCenter, new Color(0.85f, 0.75f, 0.35f));

            // Sous-titre
            var tagline = NewRect("Tagline", canvas.transform);
            SetRect(tagline, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 110f), new Vector2(900f, 40f));
            AddText(tagline.gameObject, "L'Autel des Âmes vous attend", 26, TextAnchor.MiddleCenter,
                    new Color(0.6f, 0.62f, 0.7f));

            BuildButton(canvas.transform, "JOUER", new Vector2(0f, 0f),
                        () => ScreenFader.Instance.FadeAndLoad(_gameScene));
            BuildButton(canvas.transform, "QUITTER", new Vector2(0f, -120f), Quit);

            // Splash du champion (à droite), si un portrait est assigné.
            if (_splashPortrait != null)
            {
                var portrait = NewRect("ChampionSplash", canvas.transform);
                SetRect(portrait, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                        new Vector2(-140f, -30f), new Vector2(440f, 600f));
                var img = portrait.gameObject.AddComponent<Image>();
                img.sprite = _splashPortrait;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void BuildButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var rect = NewRect("Btn_" + label, parent);
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    pos, new Vector2(360f, 84f));
            var img = AddImage(rect.gameObject, new Color(0.16f, 0.18f, 0.24f, 1f));
            AddText(rect.gameObject, label, 36, TextAnchor.MiddleCenter, Color.white);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = new Color(0.16f, 0.18f, 0.24f, 1f);
            colors.highlightedColor = new Color(0.28f, 0.32f, 0.42f, 1f); // survol
            colors.pressedColor = new Color(0.40f, 0.34f, 0.16f, 1f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            button.onClick.AddListener(onClick);
        }

        // -------------------------------------------------------------- HELPERS
        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot,
            Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        private static Image AddImage(GameObject go, Color color)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private Text AddText(GameObject go, string content, int size, TextAnchor anchor, Color color)
        {
            var t = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
            t.SetParent(go.transform, false);
            SetRect(t, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            t.anchorMin = Vector2.zero; t.anchorMax = Vector2.one;
            t.offsetMin = Vector2.zero; t.offsetMax = Vector2.zero;
            var text = t.gameObject.AddComponent<Text>();
            text.text = content; text.font = _font; text.fontSize = size;
            text.fontStyle = FontStyle.Bold; text.alignment = anchor; text.color = color;
            text.raycastTarget = false;
            return text;
        }
    }
}
