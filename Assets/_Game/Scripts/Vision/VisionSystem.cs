using System.Collections.Generic;
using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Player;
using Twisted3v3.UI;

namespace Twisted3v3.Vision
{
    /// <summary>
    /// Vision d'équipe (perspective du joueur local). Dissimule les champions ennemis
    /// tapis dans un <see cref="BushZone"/> tant que l'équipe du joueur n'y a pas de
    /// vision (aucune unité alliée dans le buisson ni à portée). Masque le rendu et la
    /// barre de vie. Auto-créé dès qu'un buisson existe ; ne touche à aucun état de jeu.
    /// </summary>
    public sealed class VisionSystem : MonoBehaviour
    {
        private static VisionSystem _instance;

        /// <summary>Crée le système s'il n'existe pas encore (appelé par BushZone).</summary>
        public static void EnsureExists()
        {
            if (_instance != null || !Application.isPlaying) return;
            var go = new GameObject("VisionSystem");
            _instance = go.AddComponent<VisionSystem>();
        }

        [SerializeField] private float _updateInterval = 0.1f;
        [Tooltip("Un allié à cette distance d'un ennemi tapi le révèle.")]
        [SerializeField] private float _revealRadius = 4.5f;

        private Team _viewerTeam = Team.Blue;
        private float _timer;

        private sealed class View { public ChampionVisuals Visuals; public WorldHealthBar Bar; public bool Visible = true; }
        private readonly Dictionary<Champion, View> _views = new();

        private void Awake() => _instance = this;
        private void OnDestroy() { if (_instance == this) _instance = null; }

        private void Start() => ResolveViewerTeam();

        private void ResolveViewerTeam()
        {
            var pc = Object.FindFirstObjectByType<PlayerController>();
            if (pc != null && pc.TryGetComponent<Champion>(out var c)) _viewerTeam = c.Team;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = _updateInterval;

            if (BushZone.All.Count == 0) return;
            var champs = Object.FindObjectsByType<Champion>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var c in champs)
            {
                // Les morts sont gérés par le RespawnController (renderers masqués) —
                // ne surtout pas réactiver leur rendu ici.
                if (c.IsDead) continue;
                Apply(c, ComputeVisible(c, champs));
            }
        }

        /// <summary>Un ennemi est caché s'il est dans un buisson non révélé par l'équipe du joueur.</summary>
        private bool ComputeVisible(Champion c, Champion[] all)
        {
            if (c == null || c.Team == _viewerTeam || c.IsDead) return true;

            BushZone bush = BushContaining(c.transform.position);
            if (bush == null) return true; // à découvert → visible

            foreach (var other in all)
            {
                if (other == null || other.Team != _viewerTeam || other.IsDead) continue;
                if (bush.Contains(other.transform.position)) return true; // allié dans le même buisson
                if ((other.transform.position - c.transform.position).sqrMagnitude
                    <= _revealRadius * _revealRadius) return true;        // allié au contact
            }
            return false; // tapi, aucune vision alliée
        }

        private static BushZone BushContaining(Vector3 pos)
        {
            var all = BushZone.All;
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].Contains(pos)) return all[i];
            return null;
        }

        private void Apply(Champion c, bool visible)
        {
            if (!_views.TryGetValue(c, out var view))
            {
                view = new View
                {
                    // Source de vérité des renderers : restaure l'état de référence
                    // (capsule masquée sous un modèle 3D comprise) au lieu de tout activer.
                    Visuals = ChampionVisuals.Of(c),
                    Bar = c.GetComponent<WorldHealthBar>()
                };
                _views[c] = view;
            }

            if (view.Visible == visible) return; // pas de churn
            view.Visible = visible;

            if (view.Visuals != null) view.Visuals.SetVisible(visible);
            if (view.Bar != null) view.Bar.SetVisible(visible);
        }
    }
}
