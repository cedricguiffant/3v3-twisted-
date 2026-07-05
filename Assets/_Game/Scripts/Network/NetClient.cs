using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using Twisted3v3.Champions;
using Twisted3v3.Core;
using Twisted3v3.Combat;
using Twisted3v3.Economy;
using Twisted3v3.Items;
using Twisted3v3.Match;
using Twisted3v3.Progression;

namespace Twisted3v3.Net
{
    /// <summary>
    /// Client de partie. Vue miroir de la simulation de l'hôte : envoie les
    /// commandes du joueur, applique les snapshots (PV, mana, or, niveaux, score)
    /// et rejoue les casts des autres champions pour le visuel. Le champion local
    /// est prédit (déplacement/casts immédiats) puis recalé en douceur.
    /// Objet pur pompé chaque frame par le <see cref="NetRunner"/>.
    /// </summary>
    public sealed class NetClient
    {
        private NetLink _link;
        private TcpClient _tcp;
        private Task _connectTask;
        private bool _helloSent;
        private bool _disconnectNotified;

        private NetEntityIndex _index;
        private Champion _own;
        private readonly List<NetGhost> _ghosts = new();
        private readonly List<LevelSystem> _levels = new();
        private readonly List<GoldWallet> _wallets = new();

        private float _pingTimer;

        public int PlayerId { get; private set; } = -1;
        public string AssignedChampion { get; private set; } = "";
        public List<LobbyPlayer> Lobby { get; } = new();
        public bool MatchStarted { get; private set; }
        public bool IsConnected => _link != null && !_link.IsClosed;
        public float PingMs { get; private set; } = -1f;
        public Champion OwnChampion => _own;
        public NetEntityIndex Index => _index;

        public event Action LobbyChanged;
        public event Action MatchStartRequested;
        public event Action<string> Disconnected;

        // ------------------------------------------------------------ Cycle de vie
        public void Connect(string address, int port)
        {
            _tcp = new TcpClient();
            _connectTask = _tcp.ConnectAsync(address, port);
        }

        public void Stop()
        {
            _link?.Close();
            _link = null;
            try { _tcp?.Close(); } catch { }
            _tcp = null;
        }

        public void Tick()
        {
            TickConnect();
            if (_link == null) return;

            while (_link.Inbox.TryDequeue(out string line))
            {
                if (!NetProtocol.TryUnpack(line, out string type, out string json)) continue;
                try { Handle(type, json); }
                catch (Exception e) { Debug.LogWarning($"[Net] Message {type} : {e.Message}"); }
            }

            if (_link.IsClosed && !_disconnectNotified)
            {
                _disconnectNotified = true;
                Disconnected?.Invoke("Connexion à l'hôte perdue.");
            }

            // Ping toutes les 2 s (affiché par le NetHud).
            _pingTimer -= Time.unscaledDeltaTime;
            if (_pingTimer <= 0f && IsConnected)
            {
                _pingTimer = 2f;
                Send("Ping", new MsgPing { t = Time.realtimeSinceStartup });
            }
        }

        private void TickConnect()
        {
            if (_connectTask == null) return;
            if (!_connectTask.IsCompleted) return;

            var task = _connectTask;
            _connectTask = null;

            if (task.IsFaulted || _tcp == null || !_tcp.Connected)
            {
                Disconnected?.Invoke("Connexion impossible (adresse/pare-feu ?).");
                return;
            }

            _link = new NetLink(_tcp);
            if (!_helloSent)
            {
                _helloSent = true;
                Send("Hello", new MsgHello
                {
                    pseudo = GameConfig.PlayerName,
                    champion = GameConfig.SelectedChampion
                });
            }
        }

        // ------------------------------------------------------------ Envois
        internal void Send(string type, object payload) => _link?.Send(NetProtocol.Pack(type, payload));

        public void SendMove(Vector3 p) => Send("Move", new CmdMove { x = p.x, y = p.y, z = p.z });
        public void SendAttackMove(Vector3 p) => Send("AttackMove", new CmdAttackMove { x = p.x, y = p.y, z = p.z });
        public void SendStop() => Send("Stop", new CmdStop());
        public void SendRecall() => Send("Recall", new CmdRecall());
        public void SendLevelAbility(AbilitySlot slot) => Send("LevelAbility", new CmdLevelAbility { slot = (int)slot });
        public void SendBuy(int itemIndex) => Send("Buy", new CmdBuy { itemIndex = itemIndex });
        public void SendSell(int slotIndex) => Send("Sell", new CmdSell { slotIndex = slotIndex });

        public void SendAttackTarget(IDamageable target)
        {
            if (_index == null) return;
            var (kind, id) = _index.IdOfDamageable(target);
            if (kind != NetEntityIndex.KindNone) Send("AttackTarget", new CmdAttackTarget { kind = kind, id = id });
        }

        public void SendCast(AbilitySlot slot, Vector3 aim, Vector3 ground, IDamageable target)
        {
            var (tKind, tId) = _index != null ? _index.IdOfDamageable(target) : (0, -1);
            Send("Cast", new CmdCast
            {
                slot = (int)slot,
                ax = aim.x, az = aim.z,
                px = ground.x, py = ground.y, pz = ground.z,
                tKind = tKind, tId = tId
            });
        }

        // ------------------------------------------------------------ Réception
        private void Handle(string type, string json)
        {
            switch (type)
            {
                case "Welcome":
                {
                    var m = NetProtocol.Parse<MsgWelcome>(json);
                    PlayerId = m.playerId;
                    AssignedChampion = m.champion;
                    // Le serveur a pu réattribuer un champion déjà pris.
                    GameConfig.SelectedChampion = m.champion;
                    LobbyChanged?.Invoke();
                    break;
                }
                case "Lobby":
                {
                    var m = NetProtocol.Parse<MsgLobby>(json);
                    Lobby.Clear();
                    Lobby.AddRange(m.players);
                    LobbyChanged?.Invoke();
                    break;
                }
                case "Start":
                    MatchStarted = true;
                    MatchStartRequested?.Invoke();
                    break;
                case "Kicked":
                {
                    var m = NetProtocol.Parse<MsgKicked>(json);
                    _disconnectNotified = true;
                    Disconnected?.Invoke(m.reason);
                    Stop();
                    break;
                }
                case "Snapshot":
                    if (_index != null) ApplySnapshot(NetProtocol.Parse<MsgSnapshot>(json));
                    break;
                case "Cast":
                    if (_index != null) ApplyCast(NetProtocol.Parse<EvtCast>(json));
                    break;
                case "AbilityLeveled":
                {
                    var m = NetProtocol.Parse<EvtAbilityLeveled>(json);
                    var champ = _index?.Resolve(NetEntityIndex.KindChampion, m.champId) as Champion;
                    champ?.Abilities.NetworkSyncRank((AbilitySlot)m.slot, m.rank);
                    break;
                }
                case "Item":
                    if (_index != null) ApplyItem(NetProtocol.Parse<EvtItem>(json));
                    break;
                case "Pong":
                {
                    var m = NetProtocol.Parse<MsgPong>(json);
                    PingMs = (Time.realtimeSinceStartup - m.t) * 1000f;
                    break;
                }
            }
        }

        // ------------------------------------------------------------ Scène de match
        /// <summary>
        /// Appelé une frame après le chargement de la map : coupe toute la
        /// simulation locale (IA, tours, respawn) et branche la vue réseau —
        /// champion local piloté par <see cref="NetClientController"/>, champions
        /// distants interpolés par <see cref="NetGhost"/>.
        /// </summary>
        public void OnMatchSceneReady()
        {
            _index = NetEntityIndex.Build();
            if (_index.Champions.Count == 0) { _index = null; return; }

            NetMatchSetup.ApplyArenaRules();
            var settings = NetMatchSetup.StripLocalControl(_index.Champions);

            _own = _index.FindChampionByName(AssignedChampion);
            _ghosts.Clear();
            _levels.Clear();
            _wallets.Clear();

            foreach (var c in _index.Champions)
            {
                _levels.Add(c.GetComponent<LevelSystem>());
                _wallets.Add(c.GetComponent<GoldWallet>());

                // Les vitals viennent des snapshots ; pas de dégâts/régén locaux.
                c.NetworkDriven = true;
                var wallet = c.GetComponent<GoldWallet>();
                if (wallet != null) wallet.NetworkDriven = true;

                var ghost = c.gameObject.AddComponent<NetGhost>();
                ghost.IsLocal = c == _own;
                _ghosts.Add(ghost);

                if (c != _own)
                {
                    // Champions distants : pas de sim locale, position interpolée.
                    var agent = c.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null) agent.enabled = false;
                    var autoAttack = c.GetComponent<AutoAttack>();
                    if (autoAttack != null) UnityEngine.Object.Destroy(autoAttack);
                }
            }

            if (_own != null)
            {
                var controller = _own.gameObject.AddComponent<NetClientController>();
                controller.Configure(this, settings);
                NetMatchSetup.BindUi(_own);
            }
            else
            {
                Debug.LogWarning($"[Net] Champion « {AssignedChampion} » introuvable côté client.");
            }

            if (MatchManager.Instance != null) MatchManager.Instance.NetworkDriven = true;
            NetHud.Ensure(this);
            Debug.Log($"[Net] Vue client prête — champion local : {(_own != null ? _own.name : "aucun")}.");
        }

        // ------------------------------------------------------------ Application de l'état
        private void ApplySnapshot(MsgSnapshot snap)
        {
            foreach (var s in snap.champs)
            {
                if (s.id < 0 || s.id >= _index.Champions.Count) continue;
                var c = _index.Champions[s.id];
                if (c == null) continue;

                c.NetworkSetVitals(s.hp, s.mana);
                while (c.Level < s.level && c.LevelUp()) { }
                _levels[s.id]?.NetworkSync(s.xp, s.skillPoints);
                _wallets[s.id]?.NetworkSet(s.gold);
                _ghosts[s.id]?.SetDead(s.dead);

                var target = new Vector3(s.x, s.y, s.z);
                if (c == _own)
                {
                    // Prédiction locale : on ne recale que si on diverge trop.
                    if (!s.dead && (c.transform.position - target).sqrMagnitude > 9f)
                        c.Motor.Warp(target);
                }
                else
                {
                    _ghosts[s.id]?.SetTarget(target, s.rotY);
                }
            }

            foreach (var s in snap.structs)
            {
                if (s.id < 0 || s.id >= _index.Structures.Count) continue;
                var structure = _index.Structures[s.id];
                if (structure == null) continue;
                if (s.dead) structure.NetworkDestroy();
                else structure.NetworkSetHealth(s.hp);
            }

            var match = MatchManager.Instance;
            if (match != null)
            {
                match.NetworkApply(snap.blueKills, snap.redKills, snap.elapsed);
                if (snap.ended) match.NetworkEnd((Team)snap.winner);
            }
        }

        private void ApplyCast(EvtCast e)
        {
            var champ = _index.Resolve(NetEntityIndex.KindChampion, e.champId) as Champion;
            if (champ == null || champ == _own) return; // le local a déjà prédit
            var target = _index.Resolve(e.tKind, e.tId);
            champ.Abilities.ForceCast((AbilitySlot)e.slot, e.rank,
                new Vector3(e.ax, 0f, e.az), new Vector3(e.px, e.py, e.pz), target);
        }

        private void ApplyItem(EvtItem e)
        {
            var champ = _index.Resolve(NetEntityIndex.KindChampion, e.champId) as Champion;
            if (champ == null) return;
            var inventory = champ.GetComponent<Inventory>();
            var wallet = champ.GetComponent<GoldWallet>();

            if (e.sold) inventory?.NetworkSellAt(e.slotIndex);
            else inventory?.NetworkBuy(NetShop.Resolve(e.itemIndex));
            wallet?.NetworkSet(e.gold);
        }
    }
}
