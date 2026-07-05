using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Twisted3v3.Core;
using Twisted3v3.Net;

namespace Twisted3v3.UI
{
    /// <summary>
    /// Menu principal : titre + choix du mode de jeu. Panneau principal
    /// (COOP VS IA / MULTIJOUEUR / QUITTER), sous-panneau multijoueur
    /// (pseudo + adresse, Héberger / Rejoindre), sélection de champion partagée
    /// entre les modes, et lobby réseau (liste des joueurs, lancement par
    /// l'hôte). Le mode choisi est enregistré dans <see cref="GameConfig"/> et
    /// lu par la scène de match. Auto-construit son Canvas.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string _gameScene = "Map_3v3";
        [SerializeField] private string _title = "TWISTED 3v3";
        [Tooltip("Splash du champion affiché à droite du menu.")]
        [SerializeField] private Sprite _splashPortrait;

        private Font _font;
        private GameObject _mainPanel;
        private GameObject _mpPanel;
        private GameObject _championPanel;
        private GameObject _lobbyPanel;
        private InputField _addressField;
        private InputField _pseudoField;
        private Text _mpStatus;
        private Text _lobbyPlayersText;
        private Text _lobbyStatus;
        private GameObject _lobbyLaunchButton;

        /// <summary>Suite donnée au choix d'un champion (solo, héberger, rejoindre).</summary>
        private System.Action<string> _onChampionPicked;

        private NetServer _server;
        private NetClient _client;

        /// <summary>Roster de l'écran de sélection : nom (= ChampionData.DisplayName),
        /// couleur signature, rôle affiché, équipe de départ dans Map_3v3.</summary>
        private static readonly (string Name, Color Color, string Role, Team Team)[] Roster =
        {
            ("Kaelthar", new Color(0.45f, 0.25f, 0.70f), "Bruiser du Vide", Team.Blue),
            ("Lirael",   new Color(0.95f, 0.85f, 0.45f), "Tireuse céleste", Team.Blue),
            ("Sylvara",  new Color(0.30f, 0.75f, 0.40f), "Mage sylvestre",  Team.Blue),
            ("Ragnor",   new Color(0.85f, 0.30f, 0.12f), "Colosse de lave", Team.Red),
            ("Vexor",    new Color(0.50f, 0.20f, 0.60f), "Assassin d'ombre", Team.Red),
            ("Tharok",   new Color(0.55f, 0.55f, 0.62f), "Tank de pierre",  Team.Red),
        };

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            EnsureEventSystem();
            Build();
        }

        private void OnDestroy() => UnhookNetwork(shutdown: false);

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
            SetRect(title, Center, Center, Center, new Vector2(0f, 200f), new Vector2(900f, 140f));
            AddText(title.gameObject, _title, 84, TextAnchor.MiddleCenter, new Color(0.85f, 0.75f, 0.35f));

            var tagline = NewRect("Tagline", canvas.transform);
            SetRect(tagline, Center, Center, Center, new Vector2(0f, 130f), new Vector2(900f, 40f));
            AddText(tagline.gameObject, "L'Autel des Âmes vous attend", 26, TextAnchor.MiddleCenter,
                    new Color(0.6f, 0.62f, 0.7f));

            _mainPanel = BuildMainPanel(canvas.transform);
            _mpPanel = BuildMultiplayerPanel(canvas.transform);
            _championPanel = BuildChampionSelectPanel(canvas.transform);
            _lobbyPanel = BuildLobbyPanel(canvas.transform);
            ShowPanel(_mainPanel);

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

        /// <summary>Affiche un seul panneau à la fois (main / mp / champions / lobby).</summary>
        private void ShowPanel(GameObject panel)
        {
            _mainPanel.SetActive(panel == _mainPanel);
            _mpPanel.SetActive(panel == _mpPanel);
            _championPanel.SetActive(panel == _championPanel);
            _lobbyPanel.SetActive(panel == _lobbyPanel);
        }

        // ---------------------------------------------------------- Panneau principal
        private GameObject BuildMainPanel(Transform parent)
        {
            var panel = NewRect("MainPanel", parent);
            SetStretch(panel);

            BuildButton(panel, "COOP VS IA", new Vector2(0f, 20f), () =>
            {
                _onChampionPicked = StartCoopVsAI;
                ShowPanel(_championPanel);
            });
            BuildButton(panel, "MULTIJOUEUR", new Vector2(0f, -80f), () =>
            {
                if (_mpStatus != null) _mpStatus.text = "";
                ShowPanel(_mpPanel);
            });
            BuildButton(panel, "QUITTER", new Vector2(0f, -180f), Quit);
            return panel.gameObject;
        }

        private void StartCoopVsAI(string championName)
        {
            GameConfig.Mode = GameMode.CoopVsAI;
            GameConfig.Role = NetRole.None;
            GameConfig.SelectedChampion = championName;
            ScreenFader.Instance.FadeAndLoad(_gameScene);
        }

        // -------------------------------------------------- Sélection de champion
        private GameObject BuildChampionSelectPanel(Transform parent)
        {
            var panel = NewRect("ChampionSelectPanel", parent);
            SetStretch(panel);

            var heading = NewRect("CsHeading", panel);
            SetRect(heading, Center, Center, Center, new Vector2(0f, 250f), new Vector2(900f, 44f));
            AddText(heading.gameObject, "CHOISIS TON CHAMPION", 38, TextAnchor.MiddleCenter,
                    new Color(0.85f, 0.75f, 0.35f));

            // Grille 3 × 2 de cartes champion.
            const float cardW = 300f, cardH = 170f, gap = 24f;
            for (int i = 0; i < Roster.Length; i++)
            {
                int col = i % 3, row = i / 3;
                var pos = new Vector2((col - 1) * (cardW + gap), 90f - row * (cardH + gap));
                BuildChampionCard(panel, Roster[i], pos, new Vector2(cardW, cardH));
            }

            BuildButton(panel, "RETOUR", new Vector2(0f, -290f), () =>
            {
                _onChampionPicked = null;
                ShowPanel(_mainPanel);
            });
            return panel.gameObject;
        }

        private void BuildChampionCard(RectTransform parent,
            (string Name, Color Color, string Role, Team Team) champ, Vector2 pos, Vector2 size)
        {
            var card = NewRect("Card_" + champ.Name, parent);
            SetRect(card, Center, Center, Center, pos, size);
            var bg = AddImage(card.gameObject, new Color(0.12f, 0.13f, 0.18f, 1f));

            // Bandeau couleur signature en haut de la carte.
            var band = NewRect("Band", card);
            SetRect(band, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    Vector2.zero, new Vector2(0f, 64f));
            AddImage(band.gameObject, champ.Color);

            // Nom + rôle + équipe de départ.
            var nameRect = NewRect("Name", card);
            SetRect(nameRect, Center, Center, Center, new Vector2(0f, -14f), new Vector2(size.x, 36f));
            AddText(nameRect.gameObject, champ.Name, 30, TextAnchor.MiddleCenter, Color.white);

            var roleRect = NewRect("Role", card);
            SetRect(roleRect, Center, Center, Center, new Vector2(0f, -44f), new Vector2(size.x, 24f));
            AddText(roleRect.gameObject, champ.Role, 18, TextAnchor.MiddleCenter,
                    new Color(0.65f, 0.68f, 0.75f));

            var teamRect = NewRect("Team", card);
            SetRect(teamRect, Center, Center, Center, new Vector2(0f, -68f), new Vector2(size.x, 22f));
            bool blue = champ.Team == Team.Blue;
            AddText(teamRect.gameObject, blue ? "Équipe Bleue" : "Équipe Rouge", 16,
                    TextAnchor.MiddleCenter,
                    blue ? new Color(0.45f, 0.65f, 1f) : new Color(1f, 0.45f, 0.4f));

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            string captured = champ.Name;
            button.onClick.AddListener(() => OnChampionPicked(captured));
        }

        private void OnChampionPicked(string championName)
        {
            var next = _onChampionPicked ?? StartCoopVsAI;
            _onChampionPicked = null;
            next(championName);
        }

        // ------------------------------------------------------- Panneau multijoueur
        private GameObject BuildMultiplayerPanel(Transform parent)
        {
            var panel = NewRect("MultiplayerPanel", parent);
            SetStretch(panel);

            var heading = NewRect("MpHeading", panel);
            SetRect(heading, Center, Center, Center, new Vector2(0f, 90f), new Vector2(700f, 40f));
            AddText(heading.gameObject, "MULTIJOUEUR", 34, TextAnchor.MiddleCenter, new Color(0.8f, 0.82f, 0.9f));

            // Pseudo
            var pseudoRect = NewRect("PseudoField", panel);
            SetRect(pseudoRect, Center, Center, Center, new Vector2(0f, 30f), new Vector2(520f, 56f));
            _pseudoField = BuildInputField(pseudoRect, GameConfig.PlayerName, "Votre pseudo…");

            BuildButton(panel, "HÉBERGER", new Vector2(-190f, -60f), HostFlow);
            BuildButton(panel, "REJOINDRE", new Vector2(190f, -60f), JoinFlow);

            // Champ d'adresse IP de l'hôte (port optionnel : « ip:port »)
            var fieldRect = NewRect("AddressField", panel);
            SetRect(fieldRect, Center, Center, Center, new Vector2(0f, -150f), new Vector2(520f, 56f));
            _addressField = BuildInputField(fieldRect, GameConfig.JoinAddress, "Adresse IP de l'hôte…");

            _mpStatus = NewText(panel, new Vector2(0f, -215f), new Vector2(900f, 34f), 22,
                                TextAnchor.MiddleCenter, new Color(0.85f, 0.7f, 0.4f));
            _mpStatus.text = "";

            BuildButton(panel, "RETOUR", new Vector2(0f, -290f), () => ShowPanel(_mainPanel));
            return panel.gameObject;
        }

        private void ApplyIdentity()
        {
            if (_pseudoField != null && !string.IsNullOrWhiteSpace(_pseudoField.text))
                GameConfig.PlayerName = _pseudoField.text.Trim();
            if (_addressField != null && !string.IsNullOrWhiteSpace(_addressField.text))
                GameConfig.JoinAddress = _addressField.text.Trim();
        }

        private void HostFlow()
        {
            ApplyIdentity();
            _onChampionPicked = StartHostWith;
            ShowPanel(_championPanel);
        }

        private void JoinFlow()
        {
            ApplyIdentity();
            if (string.IsNullOrWhiteSpace(GameConfig.JoinAddress))
            {
                _mpStatus.text = "Entrez l'adresse IP de l'hôte.";
                return;
            }
            _onChampionPicked = StartJoinWith;
            ShowPanel(_championPanel);
        }

        private void StartHostWith(string championName)
        {
            GameConfig.Mode = GameMode.Multiplayer;
            GameConfig.Role = NetRole.Host;
            GameConfig.SelectedChampion = championName;

            try
            {
                _server = NetRunner.Ensure().StartServer(GameConfig.Port);
            }
            catch (System.Exception e)
            {
                _mpStatus.text = $"Impossible d'héberger : {e.Message}";
                ShowPanel(_mpPanel);
                return;
            }

            _server.LobbyChanged += RefreshLobby;
            _lobbyLaunchButton.SetActive(true);
            _lobbyStatus.text = $"En attente de joueurs — port {GameConfig.Port}. " +
                                "Vous pouvez lancer seul (les champions libres restent des bots).";
            ShowPanel(_lobbyPanel);
            RefreshLobby();
        }

        private void StartJoinWith(string championName)
        {
            GameConfig.Mode = GameMode.Multiplayer;
            GameConfig.Role = NetRole.Client;
            GameConfig.SelectedChampion = championName;

            ParseAddress(GameConfig.JoinAddress, out string host, out int port);
            _client = NetRunner.Ensure().StartClient(host, port);
            _client.LobbyChanged += RefreshLobby;
            _client.Disconnected += OnClientDisconnected;
            _client.MatchStartRequested += LaunchMatchAsClient;

            _lobbyLaunchButton.SetActive(false);
            _lobbyStatus.text = $"Connexion à {host}:{port}…";
            ShowPanel(_lobbyPanel);
            RefreshLobby();
        }

        private static void ParseAddress(string address, out string host, out int port)
        {
            host = address;
            port = GameConfig.Port;
            int sep = address.LastIndexOf(':');
            if (sep > 0 && int.TryParse(address.Substring(sep + 1), out int parsed))
            {
                host = address.Substring(0, sep);
                port = parsed;
            }
        }

        // ------------------------------------------------------------------ Lobby
        private GameObject BuildLobbyPanel(Transform parent)
        {
            var panel = NewRect("LobbyPanel", parent);
            SetStretch(panel);

            var heading = NewRect("LobbyHeading", panel);
            SetRect(heading, Center, Center, Center, new Vector2(0f, 190f), new Vector2(700f, 44f));
            AddText(heading.gameObject, "LOBBY", 38, TextAnchor.MiddleCenter, new Color(0.85f, 0.75f, 0.35f));

            _lobbyPlayersText = NewText(panel, new Vector2(0f, 20f), new Vector2(760f, 280f), 26,
                                        TextAnchor.UpperCenter, new Color(0.85f, 0.87f, 0.95f));
            _lobbyPlayersText.text = "";

            _lobbyStatus = NewText(panel, new Vector2(0f, -160f), new Vector2(980f, 60f), 20,
                                   TextAnchor.MiddleCenter, new Color(0.85f, 0.7f, 0.4f));
            _lobbyStatus.text = "";

            var launchHolder = NewRect("LaunchHolder", panel);
            SetStretch(launchHolder);
            BuildButton(launchHolder, "LANCER LA PARTIE", new Vector2(0f, -230f), LaunchMatchAsHost);
            _lobbyLaunchButton = launchHolder.gameObject;

            BuildButton(panel, "RETOUR", new Vector2(0f, -330f), LeaveLobby);
            return panel.gameObject;
        }

        private void RefreshLobby()
        {
            if (_lobbyPlayersText == null) return;

            var sb = new System.Text.StringBuilder();
            if (_server != null)
            {
                foreach (var p in _server.BuildLobby())
                    sb.AppendLine($"{p.pseudo} — {p.champion}{(p.isHost ? "  (hôte)" : "")}");
            }
            else if (_client != null)
            {
                if (_client.Lobby.Count == 0 && _client.PlayerId < 0)
                {
                    sb.AppendLine("Connexion…");
                }
                else
                {
                    foreach (var p in _client.Lobby)
                        sb.AppendLine($"{p.pseudo} — {p.champion}{(p.isHost ? "  (hôte)" : "")}");
                    _lobbyStatus.text = "En attente du lancement par l'hôte…";
                }
            }
            _lobbyPlayersText.text = sb.ToString();
        }

        private void LaunchMatchAsHost()
        {
            if (_server == null) return;
            _server.BroadcastStartMatch();
            UnhookNetwork(shutdown: false); // le NetRunner survit au changement de scène
            ScreenFader.Instance.FadeAndLoad(_gameScene);
        }

        private void LaunchMatchAsClient()
        {
            UnhookNetwork(shutdown: false);
            ScreenFader.Instance.FadeAndLoad(_gameScene);
        }

        private void OnClientDisconnected(string reason)
        {
            UnhookNetwork();
            _mpStatus.text = reason;
            ShowPanel(_mpPanel);
        }

        private void LeaveLobby()
        {
            UnhookNetwork();
            ShowPanel(_mpPanel);
        }

        /// <summary>Détache les événements réseau du menu (et coupe le réseau si demandé).</summary>
        private void UnhookNetwork(bool shutdown = true)
        {
            if (_server != null)
            {
                _server.LobbyChanged -= RefreshLobby;
                _server = null;
                if (shutdown) NetRunner.Instance?.Shutdown();
            }
            if (_client != null)
            {
                _client.LobbyChanged -= RefreshLobby;
                _client.Disconnected -= OnClientDisconnected;
                _client.MatchStartRequested -= LaunchMatchAsClient;
                _client = null;
                if (shutdown) NetRunner.Instance?.Shutdown();
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

        // ----------------------------------------------------------------- Widgets
        private void BuildButton(RectTransform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var rect = NewRect("Btn_" + label, parent);
            SetRect(rect, Center, Center, Center, pos, new Vector2(360f, 84f));
            var img = AddImage(rect.gameObject, new Color(0.16f, 0.18f, 0.24f, 1f));
            AddText(rect.gameObject, label, 34, TextAnchor.MiddleCenter, Color.white);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = new Color(0.16f, 0.18f, 0.24f, 1f);
            colors.highlightedColor = new Color(0.28f, 0.32f, 0.42f, 1f);
            colors.pressedColor = new Color(0.40f, 0.34f, 0.16f, 1f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            button.onClick.AddListener(onClick);
        }

        private InputField BuildInputField(RectTransform rect, string value, string placeholder)
        {
            AddImage(rect.gameObject, new Color(0.1f, 0.11f, 0.15f, 1f));
            var input = rect.gameObject.AddComponent<InputField>();

            var ph = NewText(rect, Vector2.zero, Vector2.zero, 26, TextAnchor.MiddleLeft,
                             new Color(0.5f, 0.5f, 0.55f));
            StretchPadded((RectTransform)ph.transform.parent, (RectTransform)ph.transform);
            ph.text = placeholder; ph.fontStyle = FontStyle.Italic;

            var text = NewText(rect, Vector2.zero, Vector2.zero, 26, TextAnchor.MiddleLeft, Color.white);
            StretchPadded((RectTransform)text.transform.parent, (RectTransform)text.transform);
            text.supportRichText = false;

            input.textComponent = text;
            input.placeholder = ph;
            input.text = value;
            input.lineType = InputField.LineType.SingleLine;
            input.contentType = InputField.ContentType.Standard;
            return input;
        }

        // -------------------------------------------------------------- HELPERS bas niveau
        private static readonly Vector2 Center = new(0.5f, 0.5f);

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

        private static void StretchPadded(RectTransform parent, RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(14f, 0f); rt.offsetMax = new Vector2(-14f, 0f);
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

        /// <summary>Texte qui remplit son parent (utilisé dans les boutons).</summary>
        private Text AddText(GameObject go, string content, int size, TextAnchor anchor, Color color)
        {
            var t = NewRect("Text", go.transform);
            t.anchorMin = Vector2.zero; t.anchorMax = Vector2.one;
            t.offsetMin = Vector2.zero; t.offsetMax = Vector2.zero;
            var text = t.gameObject.AddComponent<Text>();
            text.text = content; text.font = _font; text.fontSize = size;
            text.fontStyle = FontStyle.Bold; text.alignment = anchor; text.color = color;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Texte positionné librement dans un parent (labels, statut).</summary>
        private Text NewText(RectTransform parent, Vector2 pos, Vector2 size, int fontSize,
                             TextAnchor anchor, Color color)
        {
            var t = NewRect("Text", parent);
            SetRect(t, Center, Center, Center, pos, size);
            var text = t.gameObject.AddComponent<Text>();
            text.font = _font; text.fontSize = fontSize; text.alignment = anchor; text.color = color;
            text.raycastTarget = false;
            return text;
        }
    }
}
