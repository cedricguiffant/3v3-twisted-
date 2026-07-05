using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Twisted3v3.Net
{
    /// <summary>
    /// Petit HUD réseau en partie : ping en haut à droite, et écran de
    /// déconnexion (message + bouton MENU) si la liaison avec l'hôte casse.
    /// Auto-construit son Canvas, comme les autres UI du projet.
    /// </summary>
    public sealed class NetHud : MonoBehaviour
    {
        private const string MenuScene = "01_MainMenu";

        private NetClient _client;
        private Text _pingText;
        private GameObject _disconnectPanel;
        private Text _disconnectText;
        private float _refreshTimer;

        public static void Ensure(NetClient client)
        {
            var existing = Object.FindFirstObjectByType<NetHud>();
            if (existing != null) { existing._client = client; return; }
            var go = new GameObject("NetHud");
            go.AddComponent<NetHud>()._client = client;
        }

        private void Start()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                       ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            var canvasGo = new GameObject("NetHud_Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60; // au-dessus du HUD de match
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            // Ping (haut droite)
            var pingGo = new GameObject("Ping", typeof(RectTransform));
            pingGo.transform.SetParent(canvasGo.transform, false);
            var pingRt = (RectTransform)pingGo.transform;
            pingRt.anchorMin = pingRt.anchorMax = pingRt.pivot = new Vector2(1f, 1f);
            pingRt.anchoredPosition = new Vector2(-16f, -10f);
            pingRt.sizeDelta = new Vector2(240f, 30f);
            _pingText = pingGo.AddComponent<Text>();
            _pingText.font = font;
            _pingText.fontSize = 20;
            _pingText.alignment = TextAnchor.UpperRight;
            _pingText.color = new Color(0.7f, 0.75f, 0.85f, 0.9f);
            _pingText.raycastTarget = false;

            // Panneau de déconnexion (caché)
            _disconnectPanel = new GameObject("DisconnectPanel", typeof(RectTransform));
            _disconnectPanel.transform.SetParent(canvasGo.transform, false);
            var panelRt = (RectTransform)_disconnectPanel.transform;
            panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;
            var dim = _disconnectPanel.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.75f);

            var msgGo = new GameObject("Message", typeof(RectTransform));
            msgGo.transform.SetParent(panelRt, false);
            var msgRt = (RectTransform)msgGo.transform;
            msgRt.anchorMin = msgRt.anchorMax = msgRt.pivot = new Vector2(0.5f, 0.5f);
            msgRt.anchoredPosition = new Vector2(0f, 60f);
            msgRt.sizeDelta = new Vector2(1000f, 60f);
            _disconnectText = msgGo.AddComponent<Text>();
            _disconnectText.font = font;
            _disconnectText.fontSize = 38;
            _disconnectText.fontStyle = FontStyle.Bold;
            _disconnectText.alignment = TextAnchor.MiddleCenter;
            _disconnectText.color = new Color(0.95f, 0.6f, 0.4f);

            var btnGo = new GameObject("Btn_Menu", typeof(RectTransform));
            btnGo.transform.SetParent(panelRt, false);
            var btnRt = (RectTransform)btnGo.transform;
            btnRt.anchorMin = btnRt.anchorMax = btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = new Vector2(0f, -40f);
            btnRt.sizeDelta = new Vector2(300f, 70f);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.16f, 0.18f, 0.24f, 1f);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(BackToMenu);

            var btnTextGo = new GameObject("Text", typeof(RectTransform));
            btnTextGo.transform.SetParent(btnRt, false);
            var btnTextRt = (RectTransform)btnTextGo.transform;
            btnTextRt.anchorMin = Vector2.zero; btnTextRt.anchorMax = Vector2.one;
            btnTextRt.offsetMin = Vector2.zero; btnTextRt.offsetMax = Vector2.zero;
            var btnText = btnTextGo.AddComponent<Text>();
            btnText.font = font;
            btnText.fontSize = 30;
            btnText.fontStyle = FontStyle.Bold;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
            btnText.text = "MENU";

            _disconnectPanel.SetActive(false);

            if (_client != null) _client.Disconnected += OnDisconnected;
        }

        private void OnDestroy()
        {
            if (_client != null) _client.Disconnected -= OnDisconnected;
        }

        private void Update()
        {
            if (_pingText == null) return;
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = 0.5f;

            if (_client != null)
                _pingText.text = _client.PingMs >= 0f
                    ? $"MULTIJOUEUR — ping {_client.PingMs:0} ms"
                    : "MULTIJOUEUR";
            else
                _pingText.text = "MULTIJOUEUR — HÔTE";
        }

        private void OnDisconnected(string reason)
        {
            if (_disconnectPanel == null) return;
            _disconnectText.text = reason;
            _disconnectPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        private void BackToMenu()
        {
            Time.timeScale = 1f;
            NetRunner.Instance?.Shutdown();
            SceneManager.LoadScene(MenuScene);
        }
    }
}
