using System;
using System.Collections.Generic;
using UnityEngine;

namespace Twisted3v3.Net
{
    /// <summary>
    /// Protocole réseau du mode multijoueur : messages JSON (JsonUtility) encadrés
    /// en lignes <c>"Type|json\n"</c> sur TCP. Hôte autoritaire : les clients
    /// envoient des *commandes* (Cmd*), le serveur renvoie des *snapshots* et des
    /// *événements* (Evt*). Aucune dépendance externe — compile partout.
    /// </summary>
    public static class NetProtocol
    {
        public const int DefaultPort = 7777;

        /// <summary>Fréquence d'envoi des snapshots serveur → clients.</summary>
        public const float SnapshotInterval = 1f / 15f;

        public static string Pack(string type, object payload) =>
            type + "|" + JsonUtility.ToJson(payload);

        public static bool TryUnpack(string line, out string type, out string json)
        {
            type = null; json = null;
            if (string.IsNullOrEmpty(line)) return false;
            int sep = line.IndexOf('|');
            if (sep <= 0) return false;
            type = line.Substring(0, sep);
            json = line.Substring(sep + 1);
            return true;
        }

        public static T Parse<T>(string json) => JsonUtility.FromJson<T>(json);
    }

    // ------------------------------------------------------------------ Lobby
    [Serializable] public class MsgHello { public string pseudo; public string champion; }

    [Serializable] public class LobbyPlayer
    {
        public int id;
        public string pseudo;
        public string champion;
        public bool isHost;
    }

    [Serializable] public class MsgWelcome { public int playerId; public string champion; }
    [Serializable] public class MsgLobby { public List<LobbyPlayer> players = new(); }
    [Serializable] public class MsgStart { }
    [Serializable] public class MsgKicked { public string reason; }

    // --------------------------------------------------- Commandes (client → serveur)
    [Serializable] public class CmdMove { public float x, y, z; }
    [Serializable] public class CmdAttackMove { public float x, y, z; }
    [Serializable] public class CmdAttackTarget { public int kind, id; }
    [Serializable] public class CmdStop { }
    [Serializable] public class CmdRecall { }

    [Serializable] public class CmdCast
    {
        public int slot;
        public float ax, az;         // direction visée (plan horizontal)
        public float px, py, pz;     // point au sol visé
        public int tKind, tId;       // cible unité éventuelle
    }

    [Serializable] public class CmdLevelAbility { public int slot; }
    [Serializable] public class CmdBuy { public int itemIndex; }
    [Serializable] public class CmdSell { public int slotIndex; }
    [Serializable] public class MsgPing { public float t; }
    [Serializable] public class MsgPong { public float t; }

    // --------------------------------------------------- État (serveur → clients)
    [Serializable]
    public class ChampState
    {
        public int id;
        public float x, y, z, rotY;
        public float hp, maxHp, mana, maxMana;
        public float xp;
        public int level, skillPoints, gold;
        public bool dead;
    }

    [Serializable]
    public class StructState
    {
        public int id;
        public float hp;
        public bool dead;
    }

    [Serializable]
    public class MsgSnapshot
    {
        public float elapsed;
        public int blueKills, redKills;
        public bool ended;
        public int winner; // Team casté en int
        public List<ChampState> champs = new();
        public List<StructState> structs = new();
    }

    /// <summary>Cast validé par le serveur, relayé aux clients pour le visuel.</summary>
    [Serializable]
    public class EvtCast
    {
        public int champId, slot, rank;
        public float ax, az, px, py, pz;
        public int tKind, tId;
    }

    [Serializable] public class EvtAbilityLeveled { public int champId, slot, rank; }

    /// <summary>Achat/vente validé par le serveur (envoyé au propriétaire du champion).</summary>
    [Serializable]
    public class EvtItem
    {
        public int champId, itemIndex, slotIndex, gold;
        public bool sold;
    }
}
