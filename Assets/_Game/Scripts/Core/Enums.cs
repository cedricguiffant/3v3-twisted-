namespace Twisted3v3.Core
{
    /// <summary>Équipes de la map 3v3.</summary>
    public enum Team
    {
        None = 0,
        Blue = 1,
        Red = 2,
        Neutral = 3 // Monstres de jungle, Autel non capturé
    }

    /// <summary>Rôle principal d'un champion (sert au matchmaking d'items / IA).</summary>
    public enum ChampionRole
    {
        Tank,
        Bruiser,
        Fighter,
        Mage,
        Support,
        Assassin,
        Marksman
    }

    /// <summary>Les 5 emplacements de capacités. Passif + Q/Z/E/R.</summary>
    public enum AbilitySlot
    {
        Passive = 0,
        Q = 1,
        Z = 2,
        E = 3,
        R = 4
    }

    /// <summary>Comment le sort est ciblé au lancement.</summary>
    public enum TargetingType
    {
        Self,        // S'applique au lanceur (ex: bouclier, dash auto)
        Direction,   // Skillshot directionnel (ex: cône, ligne)
        GroundPoint, // Zone au sol à une position (ex: mur, zone de contrôle)
        Unit         // Cible une unité précise (ex: marque, soin allié)
    }

    /// <summary>Type de dégâts — détermine quelle résistance s'applique.</summary>
    public enum DamageType
    {
        Physical,
        Magical,
        True // Ignore les résistances
    }

    /// <summary>Catégorie d'un contrôle de foule (pour les immunités / tenacity).</summary>
    public enum CrowdControlType
    {
        None,
        Slow,
        Stun,
        Root,
        Silence,
        Knockback,
        Pull,
        Airborne
    }
}
