namespace Twisted3v3.Core
{
    /// <summary>Mode de jeu choisi au menu. Lu par la scène de match au chargement.</summary>
    public enum GameMode
    {
        /// <summary>Coopératif contre l'IA : 1 joueur + alliés IA vs équipe IA (mode actuel).</summary>
        CoopVsAI,
        /// <summary>Joueur contre joueur en réseau (nécessite la couche netcode).</summary>
        Multiplayer
    }

    /// <summary>Rôle réseau pour une partie multijoueur.</summary>
    public enum NetRole { None, Host, Client }

    /// <summary>
    /// Configuration de la partie choisie au menu, persistée entre les scènes (statique).
    /// Point d'entrée unique du futur GameManager/bootstrap : la scène de match lit
    /// <see cref="Mode"/> et <see cref="Role"/> pour orchestrer le spawn des équipes.
    /// </summary>
    public static class GameConfig
    {
        public static GameMode Mode = GameMode.CoopVsAI;
        public static NetRole Role = NetRole.None;

        /// <summary>
        /// Champion choisi à l'écran de sélection (DisplayName, ex: "Kaelthar").
        /// Le <c>PlayerChampionBinder</c> lui donne le contrôle joueur au chargement
        /// de la map. Vide/introuvable → le joueur par défaut de la scène est conservé.
        /// </summary>
        public static string SelectedChampion = "Kaelthar";

        /// <summary>Adresse à rejoindre en tant que client multijoueur.</summary>
        public static string JoinAddress = "127.0.0.1";

        /// <summary>Pseudo affiché dans le lobby multijoueur.</summary>
        public static string PlayerName = "Joueur";

        /// <summary>Port TCP du mode multijoueur.</summary>
        public static int Port = 7777;

        /// <summary>
        /// Roster canonique (DisplayName des ChampionData). L'équipe découle du
        /// champion : les 3 premiers sont Bleus, les 3 derniers Rouges (Map_3v3).
        /// </summary>
        public static readonly string[] ChampionRoster =
            { "Kaelthar", "Lirael", "Sylvara", "Ragnor", "Vexor", "Tharok" };

        /// <summary>Vrai si la partie courante est une partie réseau.</summary>
        public static bool IsMultiplayer => Mode == GameMode.Multiplayer;
    }
}
