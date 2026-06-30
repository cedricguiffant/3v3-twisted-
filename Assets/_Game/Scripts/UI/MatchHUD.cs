using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Twisted3v3.Core;
using Twisted3v3.Match;

namespace Twisted3v3.UI
{
    /// <summary>
    /// HUD de partie : scoreboard (kills Bleu/Rouge + chrono) en haut, et écran de fin
    /// (vainqueur + boutons Rejouer / Menu) quand la partie se termine. Auto-construit.
    /// </summary>
    public sealed class MatchHUD : MonoBehaviour
    {
        [SerializeField] private string _gameScene = "Map_3v3";
        [SerializeField] private string _menuScene = "01_MainMenu";

        private MatchManager _match;
        private Font _font;
        private Sprite _white;

        private Text _scoreText, _timerText, _winnerText;
        private GameObject _endPanel;

        private void Start()
        {
            _match = MatchManager.Instance;
            if (_match == null) { enabled = false; return; }
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _white = MakeWhite();
            EnsureEventSystem();
            Build();

            _match.OnScoreChanged += RefreshScore;
            _match.OnMatchEnded += ShowEndScreen;
            RefreshScore();
        }

        private void OnDestroy()
        {
            if (_match == null) return;
            _match.OnScoreChanged -= RefreshScore;
            _match.OnMatchEnded -= ShowEndScreen;
        }

        private void Update()
        {
            if (_match == null || _match.IsEnded || _timerText == null) return;
            _timerText.text = FormatTime(_match.Elapsed) +
                              (_match.MatchDuration > 0f ? " / " + FormatTime(_match.MatchDuration) : "");
        }

        private void Build()
        {
            var canvas = NewCanvas("Match_Canvas");

            // Scoreboard haut-centre
            var panel = NewRect("Scoreboard", canvas.transform);
            SetRect(panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -14f), new Vector2(440f, 70f));
            AddImage(panel, new Color(0.05f, 0.06f, 0.09f, 0.85f));

            _scoreText = AddText(panel, "0  -  0", 34, new Vector2(0f, -6f), new Vector2(440f, 40f),
                                 TextAnchor.UpperCenter, Color.white);
            _timerText = AddText(panel, "00:00", 18, new Vector2(0f, -44f), new Vector2(440f, 22f),
                                 TextAnchor.UpperCenter, new Color(0.7f, 0.72f, 0.8f));

            // Écran de fin (caché)
            _endPanel = NewRect("EndScreen", canvas.transform).gameObject;
            var endRt = (RectTransform)_endPanel.transform;
            endRt.anchorMin = Vector2.zero; endRt.anchorMax = Vector2.one;
            endRt.offsetMin = Vector2.zero; endRt.offsetMax = Vector2.zero;
            AddImage(endRt, new Color(0.02f, 0.03f, 0.05f, 0.92f));

            _winnerText = AddText(endRt, "", 72, new Vector2(0f, 160f), new Vector2(1200f, 120f),
                                  TextAnchor.MiddleCenter, new Color(0.9f, 0.8f, 0.35f));
            ((RectTransform)_winnerText.transform).anchorMin = new Vector2(0.5f, 0.5f);
            ((RectTransform)_winnerText.transform).anchorMax = new Vector2(0.5f, 0.5f);

            BuildEndButton(endRt, "REJOUER", new Vector2(0f, -10f), () => Load(_gameScene));
            BuildEndButton(endRt, "MENU", new Vector2(0f, -110f), () => Load(_menuScene));

            _endPanel.SetActive(false);
        }

        private void RefreshScore()
        {
            if (_scoreText == null) return;
            _scoreText.text = $"BLEU  {_match.BlueKills}  -  {_match.RedKills}  ROUGE";
        }

        private void ShowEndScreen()
        {
            _endPanel.SetActive(true);
            _winnerText.text = _match.Winner switch
            {
                Team.Blue => "VICTOIRE — ÉQUIPE BLEUE",
                Team.Red => "VICTOIRE — ÉQUIPE ROUGE",
                _ => "ÉGALITÉ"
            };
            _winnerText.color = _match.Winner == Team.Red
                ? new Color(0.95f, 0.4f, 0.4f)
                : _match.Winner == Team.Blue ? new Color(0.45f, 0.6f, 1f) : Color.white;
        }

        private void Load(string scene)
        {
            Time.timeScale = 1f; // ré-active le temps avant de changer de scène
            ScreenFader.Instance.FadeAndLoad(scene);
        }

        // ---- helpers ----
        private Canvas NewCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return canvas;
        }

        private void BuildEndButton(RectTransform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var rect = NewRect("Btn_" + label, parent);
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    pos, new Vector2(340f, 80f));
            var img = AddImage(rect, new Color(0.16f, 0.18f, 0.24f, 1f));
            AddText(rect, label, 34, Vector2.zero, new Vector2(340f, 80f), TextAnchor.MiddleCenter, Color.white);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.28f, 0.32f, 0.42f, 1f);
            button.colors = colors;
            button.onClick.AddListener(onClick);
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
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

        private Image AddImage(RectTransform rt, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = _white; img.color = color;
            return img;
        }

        private Text AddText(RectTransform parent, string content, int size, Vector2 pos,
            Vector2 sizeDelta, TextAnchor anchor, Color color)
        {
            var rt = NewRect("Text", parent);
            SetRect(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, sizeDelta);
            var text = rt.gameObject.AddComponent<Text>();
            text.text = content; text.font = _font; text.fontSize = size; text.fontStyle = FontStyle.Bold;
            text.alignment = anchor; text.color = color; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        private static string FormatTime(float t)
        {
            int m = Mathf.FloorToInt(t / 60f);
            int s = Mathf.FloorToInt(t % 60f);
            return $"{m:00}:{s:00}";
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
