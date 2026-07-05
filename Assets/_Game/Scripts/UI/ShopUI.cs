using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Twisted3v3.Champions;
using Twisted3v3.Combat;
using Twisted3v3.Economy;
using Twisted3v3.Items;

namespace Twisted3v3.UI
{
    /// <summary>
    /// Boutique du joueur : ouverte/fermée avec une touche (P par défaut), liste les
    /// items du catalogue et permet l'achat (uniquement en base, comme un MOBA) ainsi
    /// que la revente des items équipés. Auto-construit son Canvas. Appelle
    /// l'<see cref="Inventory"/> du champion joueur — aucune logique d'économie ici.
    /// </summary>
    public sealed class ShopUI : MonoBehaviour
    {
        [SerializeField] private Champion _champion;
        [SerializeField] private ItemCatalog _catalog;
        [SerializeField] private KeyCode _toggleKey = KeyCode.P;
        [Tooltip("Si vrai, l'achat n'est possible que dans la fontaine de base alliée.")]
        [SerializeField] private bool _requireBase = true;

        private Inventory _inventory;
        private GoldWallet _wallet;
        private Font _font;
        private Sprite _white;

        private GameObject _root;
        private Text _goldText, _statusText;
        private readonly List<(ItemData item, Button button, Text label)> _rows = new();
        private readonly List<(Button button, Text label)> _slotButtons = new();
        private float _statusUntil;

        private void Start()
        {
            // Multijoueur client : le champion local est branché par la couche
            // réseau une frame après le chargement — on attend qu'il soit prêt.
            if (Core.GameConfig.IsMultiplayer && Core.GameConfig.Role == Core.NetRole.Client)
            {
                StartCoroutine(InitWhenNetworkReady());
                return;
            }
            Init();
        }

        private System.Collections.IEnumerator InitWhenNetworkReady()
        {
            float deadline = Time.unscaledTime + 5f;
            while (Time.unscaledTime < deadline)
            {
                var client = Net.NetRunner.IsClient ? Net.NetRunner.Instance.Client : null;
                if (client != null && client.OwnChampion != null)
                {
                    _champion = client.OwnChampion;
                    Init();
                    yield break;
                }
                yield return null;
            }
            enabled = false;
        }

        private void Init()
        {
            if (_champion == null) _champion = ResolvePlayerChampion();
            if (_champion == null) { enabled = false; return; }

            _inventory = _champion.GetComponent<Inventory>();
            if (_inventory == null) _inventory = _champion.gameObject.AddComponent<Inventory>();
            _wallet = _champion.GetComponent<GoldWallet>();

            if (_catalog == null) _catalog = Resources.Load<ItemCatalog>("ItemCatalog");
            Net.NetShop.Catalog = _catalog; // items identifiés par index en réseau

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _white = MakeWhite();

            Build();
            _inventory.OnChanged += RefreshSlots;
            _root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_inventory != null) _inventory.OnChanged -= RefreshSlots;
        }

        /// <summary>Champion piloté par le joueur (celui qui porte un PlayerController).</summary>
        private static Champion ResolvePlayerChampion()
        {
            foreach (var pc in Object.FindObjectsByType<Player.PlayerController>(FindObjectsSortMode.None))
            {
                var c = pc.GetComponent<Champion>();
                if (c != null) return c;
            }
            return Object.FindFirstObjectByType<Champion>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey)) Toggle();
            if (_root == null || !_root.activeSelf) return;

            if (_wallet != null) _goldText.text = $"Or : {_wallet.Gold}";

            // Actualise l'accessibilité de chaque item (or, place, unicité).
            for (int i = 0; i < _rows.Count; i++)
            {
                var (item, button, label) = _rows[i];
                var err = _inventory.CanBuy(item);
                button.interactable = err == Inventory.PurchaseError.None;
                label.color = err == Inventory.PurchaseError.None
                    ? Color.white
                    : new Color(0.55f, 0.55f, 0.6f);
            }

            if (Time.unscaledTime > _statusUntil && _statusText != null) _statusText.text = "";
        }

        public void Toggle()
        {
            if (_root == null) return;
            bool show = !_root.activeSelf;
            _root.SetActive(show);
            if (show) RefreshSlots();
        }

        // ------------------------------------------------------------------ Achat
        private void OnBuyClicked(ItemData item)
        {
            if (_requireBase && !IsInAlliedBase())
            {
                Flash("Retournez à la base pour acheter.");
                Twisted3v3.Audio.Sfx.Play2D(Twisted3v3.Audio.SfxId.Deny, 0.6f);
                return;
            }

            switch (_inventory.CanBuy(item))
            {
                case Inventory.PurchaseError.Full: Deny("Inventaire plein."); return;
                case Inventory.PurchaseError.NotEnoughGold: Deny("Or insuffisant."); return;
                case Inventory.PurchaseError.DuplicateUnique: Deny("Un seul exemplaire autorisé."); return;
            }

            // Client multijoueur : l'achat est validé par le serveur.
            if (Net.NetShop.InterceptBuy(_champion, item))
            {
                Flash($"Achat : {item.DisplayName}…");
                return;
            }

            if (_inventory.TryBuy(item))
            {
                Flash($"Acheté : {item.DisplayName}");
                Twisted3v3.Audio.Sfx.Play2D(Twisted3v3.Audio.SfxId.Buy, 0.8f);
            }
        }

        private void Deny(string msg)
        {
            Flash(msg);
            Twisted3v3.Audio.Sfx.Play2D(Twisted3v3.Audio.SfxId.Deny, 0.6f);
        }

        private bool IsInAlliedBase()
        {
            foreach (var f in Object.FindObjectsByType<FountainZone>(FindObjectsSortMode.None))
                if (f.Team == _champion.Team && f.IsInside(_champion.transform.position)) return true;
            return false;
        }

        private void Flash(string msg)
        {
            if (_statusText == null) return;
            _statusText.text = msg;
            _statusUntil = Time.unscaledTime + 2.5f;
        }

        // --------------------------------------------------------------- Construction UI
        private void Build()
        {
            var go = new GameObject("Shop_Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            _root = go;

            // Fenêtre centrale
            var panel = NewRect("ShopPanel", canvas.transform);
            SetCenter(panel, new Vector2(720f, 720f));
            AddImage(panel.gameObject, new Color(0.06f, 0.07f, 0.10f, 0.96f));

            AddText(panel, "BOUTIQUE", 28, new Vector2(20f, -14f), new Vector2(400f, 34f),
                    TextAnchor.UpperLeft, new Color(0.95f, 0.85f, 0.5f));
            _goldText = AddText(panel, "Or : 0", 20, new Vector2(-20f, -16f), new Vector2(240f, 28f),
                    TextAnchor.UpperRight, new Color(1f, 0.85f, 0.3f));
            SetRect((RectTransform)_goldText.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(1f, 1f), new Vector2(-20f, -16f), new Vector2(240f, 28f));

            // Liste des items (colonne)
            float y = -60f;
            foreach (var item in _catalog != null ? _catalog.Items : new List<ItemData>())
            {
                if (item == null) continue;
                CreateItemRow(panel, item, y);
                y -= 46f;
            }

            // Bandeau des slots équipés (bas)
            var slots = NewRect("Slots", panel);
            SetRect(slots, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 52f), new Vector2(-40f, 56f));
            for (int i = 0; i < Inventory.MaxSlots; i++)
                CreateSlot(slots, i);

            _statusText = AddText(panel, "", 18, new Vector2(20f, 14f), new Vector2(680f, 26f),
                    TextAnchor.LowerLeft, new Color(1f, 0.6f, 0.4f));

            AddText(panel, $"[{_toggleKey}] fermer", 16, new Vector2(-20f, 14f), new Vector2(200f, 24f),
                    TextAnchor.LowerRight, new Color(0.6f, 0.6f, 0.7f));
        }

        private void CreateItemRow(RectTransform parent, ItemData item, float y)
        {
            var row = NewRect($"Row_{item.name}", parent);
            SetRect(row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, y), new Vector2(-40f, 42f));
            AddImage(row.gameObject, TierColor(item.Tier) * new Color(1f, 1f, 1f, 0.25f));

            AddText(row, item.DisplayName, 18, new Vector2(12f, 0f), new Vector2(360f, 42f),
                    TextAnchor.MiddleLeft, Color.white);
            AddText(row, StatSummary(item), 13, new Vector2(300f, 0f), new Vector2(220f, 42f),
                    TextAnchor.MiddleLeft, new Color(0.7f, 0.85f, 0.9f));

            // Bouton d'achat à droite
            var btnRect = NewRect("Buy", row);
            SetRect(btnRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(-8f, 0f), new Vector2(120f, 34f));
            var img = AddImage(btnRect.gameObject, new Color(0.2f, 0.45f, 0.3f, 1f));
            var button = btnRect.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var captured = item;
            button.onClick.AddListener(() => OnBuyClicked(captured));
            var label = AddText(btnRect, $"{item.TotalCost} or", 16, Vector2.zero, new Vector2(120f, 34f),
                    TextAnchor.MiddleCenter, Color.white);
            _rows.Add((item, button, label));
        }

        private void CreateSlot(RectTransform parent, int index)
        {
            var slot = NewRect($"Slot_{index}", parent);
            SetRect(slot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(12f + index * 108f, 0f), new Vector2(100f, 48f));
            var img = AddImage(slot.gameObject, new Color(0.12f, 0.13f, 0.18f, 1f));
            var button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            int captured = index;
            button.onClick.AddListener(() => TrySell(captured));
            var label = AddText(slot, "—", 13, Vector2.zero, new Vector2(100f, 48f),
                    TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.85f));
            _slotButtons.Add((button, label));
        }

        private void TrySell(int index)
        {
            if (index >= _inventory.Items.Count) return;
            var name = _inventory.Items[index].DisplayName;

            // Client multijoueur : la vente est validée par le serveur.
            if (Net.NetShop.InterceptSell(_champion, index))
            {
                Flash($"Vente : {name}…");
                return;
            }
            if (_inventory.SellAt(index))
            {
                Flash($"Vendu : {name}");
                Twisted3v3.Audio.Sfx.Play2D(Twisted3v3.Audio.SfxId.Sell, 0.7f);
            }
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < _slotButtons.Count; i++)
            {
                bool filled = i < _inventory.Items.Count;
                _slotButtons[i].label.text = filled ? _inventory.Items[i].DisplayName : "—";
                _slotButtons[i].button.interactable = filled;
            }
        }

        private static string StatSummary(ItemData item)
        {
            if (item.Stats == null || item.Stats.Count == 0) return "";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < item.Stats.Count && i < 3; i++)
            {
                var s = item.Stats[i];
                if (i > 0) sb.Append(", ");
                // Les stats fractionnaires (CDR/crit/vol de vie... 0..1) s'affichent en %
                // même en modificateur Flat — sinon « +0.1 CDR » devient « +0 CDR ».
                bool pct = s.Modifier != Twisted3v3.Stats.ModifierType.Flat || IsFraction(s.Stat);
                sb.Append(pct ? $"+{s.Value * 100f:0}% " : $"+{s.Value:0.#} ").Append(Short(s.Stat));
            }
            return sb.ToString();
        }

        /// <summary>Stats exprimées en fraction 0..1 (affichées en pourcentage).</summary>
        private static bool IsFraction(Twisted3v3.Stats.StatType t) =>
            t is Twisted3v3.Stats.StatType.CooldownReduction
              or Twisted3v3.Stats.StatType.CritChance
              or Twisted3v3.Stats.StatType.Lifesteal
              or Twisted3v3.Stats.StatType.Omnivamp
              or Twisted3v3.Stats.StatType.Tenacity;

        private static string Short(Twisted3v3.Stats.StatType t) => t switch
        {
            Twisted3v3.Stats.StatType.MaxHealth => "PV",
            Twisted3v3.Stats.StatType.AttackDamage => "AD",
            Twisted3v3.Stats.StatType.AbilityPower => "AP",
            Twisted3v3.Stats.StatType.Armor => "Arm",
            Twisted3v3.Stats.StatType.MagicResist => "RM",
            Twisted3v3.Stats.StatType.AttackSpeed => "AS",
            Twisted3v3.Stats.StatType.MoveSpeed => "MS",
            Twisted3v3.Stats.StatType.CooldownReduction => "CDR",
            Twisted3v3.Stats.StatType.Lifesteal => "VV",
            _ => t.ToString()
        };

        private static Color TierColor(ItemTier tier) => tier switch
        {
            ItemTier.Starter => new Color(0.5f, 0.5f, 0.5f),
            ItemTier.Boots => new Color(0.4f, 0.6f, 0.8f),
            ItemTier.Legendary => new Color(0.55f, 0.4f, 0.75f),
            ItemTier.Mythic => new Color(0.85f, 0.55f, 0.25f),
            _ => Color.gray
        };

        // ------------------------------------------------------------- Helpers UI bas niveau
        private RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private void SetCenter(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }

        private void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
                             Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        private Image AddImage(GameObject go, Color color)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.sprite = _white; img.color = color; img.type = Image.Type.Sliced;
            return img;
        }

        private Text AddText(RectTransform parent, string content, int size, Vector2 pos,
                             Vector2 sizeDelta, TextAnchor anchor, Color color)
        {
            var rt = NewRect("Text", parent);
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = sizeDelta;
            var txt = rt.gameObject.AddComponent<Text>();
            txt.text = content; txt.font = _font; txt.fontSize = size;
            txt.alignment = anchor; txt.color = color; txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            return txt;
        }

        private Sprite MakeWhite()
        {
            var tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
