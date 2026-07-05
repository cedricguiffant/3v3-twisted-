using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Twisted3v3.AI;
using Twisted3v3.Champions;
using Twisted3v3.Combat;
using Twisted3v3.Core;
using Twisted3v3.Economy;
using Twisted3v3.Items;
using Twisted3v3.Match;
using Twisted3v3.Progression;

namespace Twisted3v3.Net
{
    /// <summary>
    /// Serveur de partie (côté hôte). Autoritaire : la simulation existante
    /// (champions, tours, IA des champions non joués) tourne uniquement ici ;
    /// les clients envoient des commandes appliquées à *leur* champion et
    /// reçoivent des snapshots à 15 Hz. Objet pur (pas un MonoBehaviour) —
    /// pompé chaque frame par le <see cref="NetRunner"/>.
    /// </summary>
    public sealed class NetServer
    {
        public sealed class RemotePlayer
        {
            public int Id;
            public NetLink Link;
            public string Pseudo;
            public string ChampionName = "";
            public Champion Champion;          // lié au chargement de la map
            public bool HasHello => Pseudo != null;
        }

        private TcpListener _listener;
        private readonly List<RemotePlayer> _players = new();
        private int _nextId = 1;

        private string _hostPseudo = "Hôte";
        private string _hostChampion = "";

        private NetEntityIndex _index;
        private readonly List<LevelSystem> _levels = new();
        private readonly List<GoldWallet> _wallets = new();
        private float _snapshotTimer;

        public bool MatchStarted { get; private set; }
        public bool IsListening => _listener != null;

        /// <summary>Déclenché quand la composition du lobby change (UI du menu).</summary>
        public event Action LobbyChanged;

        public IReadOnlyList<RemotePlayer> Players => _players;

        // ------------------------------------------------------------ Cycle de vie
        public void Start(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _hostPseudo = string.IsNullOrWhiteSpace(GameConfig.PlayerName) ? "Hôte" : GameConfig.PlayerName;
            _hostChampion = GameConfig.SelectedChampion;
            Debug.Log($"[Net] Serveur à l'écoute sur le port {port}.");
        }

        public void Stop()
        {
            foreach (var p in _players) p.Link?.Close();
            _players.Clear();
            try { _listener?.Stop(); } catch { }
            _listener = null;
            _index = null;
            MatchStarted = false;
        }

        public void Tick()
        {
            AcceptPending();
            PumpMessages();
            RemoveDisconnected();
            if (MatchStarted && _index != null) SendSnapshots();
        }

        // ------------------------------------------------------------ Connexions
        private void AcceptPending()
        {
            if (_listener == null) return;
            try
            {
                while (_listener.Pending())
                {
                    var link = new NetLink(_listener.AcceptTcpClient()) { Id = _nextId++ };
                    if (MatchStarted)
                    {
                        // Pas de rejoint en cours de partie (v1).
                        link.Send(NetProtocol.Pack("Kicked", new MsgKicked { reason = "Partie déjà en cours." }));
                        link.Close();
                        continue;
                    }
                    _players.Add(new RemotePlayer { Id = link.Id, Link = link });
                }
            }
            catch (Exception e) { Debug.LogWarning($"[Net] Accept : {e.Message}"); }
        }

        private void RemoveDisconnected()
        {
            for (int i = _players.Count - 1; i >= 0; i--)
            {
                var p = _players[i];
                if (!p.Link.IsClosed) continue;
                _players.RemoveAt(i);
                Debug.Log($"[Net] {p.Pseudo ?? "?"} déconnecté.");

                // En partie : son champion redevient un bot pour ne pas fausser le 3v3.
                if (MatchStarted && p.Champion != null && p.Champion.GetComponent<ChampionAI>() == null)
                    p.Champion.gameObject.AddComponent<ChampionAI>();

                BroadcastLobby();
                LobbyChanged?.Invoke();
            }
        }

        // ------------------------------------------------------------ Messages
        private void PumpMessages()
        {
            foreach (var p in _players)
            {
                while (p.Link.Inbox.TryDequeue(out string line))
                {
                    if (!NetProtocol.TryUnpack(line, out string type, out string json)) continue;
                    try { Handle(p, type, json); }
                    catch (Exception e) { Debug.LogWarning($"[Net] Message {type} : {e.Message}"); }
                }
            }
        }

        private void Handle(RemotePlayer p, string type, string json)
        {
            if (type == "Hello") { HandleHello(p, NetProtocol.Parse<MsgHello>(json)); return; }
            if (type == "Ping") { p.Link.Send(NetProtocol.Pack("Pong", new MsgPong { t = NetProtocol.Parse<MsgPing>(json).t })); return; }
            if (!p.HasHello) return;

            var champ = p.Champion;
            if (champ == null || champ.IsDead) return;
            var autoAttack = champ.GetComponent<AutoAttack>();

            switch (type)
            {
                case "Move":
                {
                    var m = NetProtocol.Parse<CmdMove>(json);
                    autoAttack?.ClearTarget();
                    champ.Motor.MoveTo(new Vector3(m.x, m.y, m.z));
                    break;
                }
                case "AttackMove":
                {
                    var m = NetProtocol.Parse<CmdAttackMove>(json);
                    autoAttack?.AttackMove(new Vector3(m.x, m.y, m.z));
                    break;
                }
                case "AttackTarget":
                {
                    var m = NetProtocol.Parse<CmdAttackTarget>(json);
                    var target = _index?.Resolve(m.kind, m.id);
                    if (target != null) autoAttack?.SetTarget(target);
                    break;
                }
                case "Stop":
                    autoAttack?.ClearTarget();
                    champ.Motor.Stop();
                    break;
                case "Recall":
                {
                    var recall = champ.GetComponent<RecallController>()
                                 ?? champ.gameObject.AddComponent<RecallController>();
                    recall.Toggle();
                    break;
                }
                case "Cast":
                {
                    var m = NetProtocol.Parse<CmdCast>(json);
                    var target = _index?.Resolve(m.tKind, m.tId);
                    champ.Abilities.TryCast((AbilitySlot)m.slot,
                        new Vector3(m.ax, 0f, m.az), new Vector3(m.px, m.py, m.pz), target);
                    break;
                }
                case "LevelAbility":
                {
                    var m = NetProtocol.Parse<CmdLevelAbility>(json);
                    var levels = champ.GetComponent<LevelSystem>();
                    if (levels != null && levels.TryLevelAbility((AbilitySlot)m.slot))
                        BroadcastAbilityLeveled(champ, (AbilitySlot)m.slot);
                    break;
                }
                case "Buy":
                {
                    var m = NetProtocol.Parse<CmdBuy>(json);
                    var item = NetShop.Resolve(m.itemIndex);
                    var inventory = champ.GetComponent<Inventory>();
                    var wallet = champ.GetComponent<GoldWallet>();
                    if (item != null && inventory != null && inventory.TryBuy(item))
                        p.Link.Send(NetProtocol.Pack("Item", new EvtItem
                        {
                            champId = _index.IdOf(champ),
                            itemIndex = m.itemIndex,
                            slotIndex = -1,
                            gold = wallet != null ? wallet.Gold : 0,
                            sold = false
                        }));
                    break;
                }
                case "Sell":
                {
                    var m = NetProtocol.Parse<CmdSell>(json);
                    var inventory = champ.GetComponent<Inventory>();
                    var wallet = champ.GetComponent<GoldWallet>();
                    if (inventory != null && inventory.SellAt(m.slotIndex))
                        p.Link.Send(NetProtocol.Pack("Item", new EvtItem
                        {
                            champId = _index.IdOf(champ),
                            itemIndex = -1,
                            slotIndex = m.slotIndex,
                            gold = wallet != null ? wallet.Gold : 0,
                            sold = true
                        }));
                    break;
                }
            }
        }

        private void HandleHello(RemotePlayer p, MsgHello hello)
        {
            p.Pseudo = string.IsNullOrWhiteSpace(hello.pseudo) ? $"Joueur {p.Id}" : hello.pseudo.Trim();
            p.ChampionName = AssignChampion(hello.champion);

            if (p.ChampionName == null)
            {
                p.Link.Send(NetProtocol.Pack("Kicked", new MsgKicked { reason = "Aucun champion libre (lobby plein)." }));
                p.Link.Close();
                return;
            }

            p.Link.Send(NetProtocol.Pack("Welcome", new MsgWelcome { playerId = p.Id, champion = p.ChampionName }));
            Debug.Log($"[Net] {p.Pseudo} a rejoint — champion : {p.ChampionName}.");
            BroadcastLobby();
            LobbyChanged?.Invoke();
        }

        /// <summary>Attribue le champion demandé s'il est libre, sinon le premier libre.</summary>
        private string AssignChampion(string wanted)
        {
            var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { _hostChampion };
            foreach (var p in _players)
                if (!string.IsNullOrEmpty(p.ChampionName)) taken.Add(p.ChampionName);

            if (!string.IsNullOrWhiteSpace(wanted) && !taken.Contains(wanted.Trim()))
                return wanted.Trim();
            foreach (var name in GameConfig.ChampionRoster)
                if (!taken.Contains(name)) return name;
            return null;
        }

        // ------------------------------------------------------------ Lobby
        public List<LobbyPlayer> BuildLobby()
        {
            var list = new List<LobbyPlayer>
            {
                new LobbyPlayer { id = 0, pseudo = _hostPseudo, champion = _hostChampion, isHost = true }
            };
            foreach (var p in _players)
                if (p.HasHello)
                    list.Add(new LobbyPlayer { id = p.Id, pseudo = p.Pseudo, champion = p.ChampionName });
            return list;
        }

        private void BroadcastLobby()
        {
            string msg = NetProtocol.Pack("Lobby", new MsgLobby { players = BuildLobby() });
            foreach (var p in _players)
                if (p.HasHello) p.Link.Send(msg);
        }

        /// <summary>Lance la partie : préviens les clients puis l'hôte charge la map.</summary>
        public void BroadcastStartMatch()
        {
            MatchStarted = true;
            string msg = NetProtocol.Pack("Start", new MsgStart());
            foreach (var p in _players)
                if (p.HasHello) p.Link.Send(msg);
        }

        // ------------------------------------------------------------ Scène de match
        /// <summary>
        /// Appelé une frame après le chargement de la map (après le
        /// PlayerChampionBinder) : lie les champions aux joueurs distants,
        /// retire leur IA et applique les règles d'arène multijoueur.
        /// </summary>
        public void OnMatchSceneReady()
        {
            _index = NetEntityIndex.Build();
            if (_index.Champions.Count == 0) { _index = null; return; }

            NetMatchSetup.ApplyArenaRules();

            _levels.Clear();
            _wallets.Clear();
            foreach (var c in _index.Champions)
            {
                _levels.Add(c.GetComponent<LevelSystem>());
                _wallets.Add(c.GetComponent<GoldWallet>());
            }

            foreach (var p in _players)
            {
                if (!p.HasHello) continue;
                p.Champion = _index.FindChampionByName(p.ChampionName);
                if (p.Champion == null)
                {
                    Debug.LogWarning($"[Net] Champion « {p.ChampionName} » introuvable pour {p.Pseudo}.");
                    continue;
                }
                // Le binder a mis tout le monde en IA sauf l'hôte : les champions
                // des joueurs distants sont pilotés par leurs commandes réseau.
                foreach (var ai in p.Champion.GetComponents<ChampionAI>())
                    UnityEngine.Object.DestroyImmediate(ai);
                var autoAttack = p.Champion.GetComponent<AutoAttack>();
                if (autoAttack != null) autoAttack.Kite = false;
            }

            // Relaye chaque cast validé (joueurs ET bots) pour le visuel des clients.
            foreach (var champion in _index.Champions)
            {
                var c = champion; // capture
                c.Abilities.OnCastNetwork += (slot, rank, aim, ground, target) =>
                    BroadcastCast(c, slot, rank, aim, ground, target);
            }

            _snapshotTimer = 0f;
            NetHud.Ensure(null); // affiche « MULTIJOUEUR — HÔTE »
            Debug.Log($"[Net] Match prêt — {_players.Count} joueur(s) distant(s).");
        }

        private void BroadcastCast(Champion caster, AbilitySlot slot, int rank,
                                   Vector3 aim, Vector3 ground, IDamageable target)
        {
            if (_index == null) return;
            var (tKind, tId) = _index.IdOfDamageable(target);
            string msg = NetProtocol.Pack("Cast", new EvtCast
            {
                champId = _index.IdOf(caster),
                slot = (int)slot,
                rank = rank,
                ax = aim.x, az = aim.z,
                px = ground.x, py = ground.y, pz = ground.z,
                tKind = tKind, tId = tId
            });
            foreach (var p in _players)
            {
                if (!p.HasHello) continue;
                if (p.Champion == caster) continue; // le propriétaire a déjà prédit son cast
                p.Link.Send(msg);
            }
        }

        private void BroadcastAbilityLeveled(Champion champ, AbilitySlot slot)
        {
            var inst = champ.Abilities.GetSlot(slot);
            string msg = NetProtocol.Pack("AbilityLeveled", new EvtAbilityLeveled
            {
                champId = _index.IdOf(champ),
                slot = (int)slot,
                rank = inst != null ? inst.Rank : 1
            });
            foreach (var p in _players)
                if (p.HasHello) p.Link.Send(msg);
        }

        // ------------------------------------------------------------ Snapshots
        private void SendSnapshots()
        {
            _snapshotTimer -= Time.unscaledDeltaTime;
            if (_snapshotTimer > 0f) return;
            _snapshotTimer = NetProtocol.SnapshotInterval;

            var match = MatchManager.Instance;
            var snap = new MsgSnapshot
            {
                elapsed = match != null ? match.Elapsed : 0f,
                blueKills = match != null ? match.BlueKills : 0,
                redKills = match != null ? match.RedKills : 0,
                ended = match != null && match.IsEnded,
                winner = match != null ? (int)match.Winner : 0
            };

            for (int i = 0; i < _index.Champions.Count; i++)
            {
                var c = _index.Champions[i];
                if (c == null) continue;
                var pos = c.transform.position;
                snap.champs.Add(new ChampState
                {
                    id = i,
                    x = pos.x, y = pos.y, z = pos.z,
                    rotY = c.transform.eulerAngles.y,
                    hp = c.Health?.CurrentHealth ?? 0f,
                    maxHp = c.Health?.MaxHealth ?? 1f,
                    mana = c.CurrentMana,
                    maxMana = c.Stats != null ? c.Stats.Value(Stats.StatType.MaxMana) : 0f,
                    xp = _levels[i] != null ? _levels[i].CurrentXp : 0f,
                    level = c.Level,
                    skillPoints = _levels[i] != null ? _levels[i].SkillPoints : 0,
                    gold = _wallets[i] != null ? _wallets[i].Gold : 0,
                    dead = c.IsDead
                });
            }

            for (int i = 0; i < _index.Structures.Count; i++)
            {
                var s = _index.Structures[i];
                if (s == null) continue;
                snap.structs.Add(new StructState { id = i, hp = s.CurrentHealth, dead = s.IsDead });
            }

            string msg = NetProtocol.Pack("Snapshot", snap);
            foreach (var p in _players)
                if (p.HasHello) p.Link.Send(msg);
        }
    }
}
