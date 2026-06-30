namespace Twisted3v3.Stats
{
    /// <summary>
    /// Toutes les statistiques d'un champion. Sert de clé dans le StatSheet.
    /// Ajoute ici toute nouvelle stat — le reste du système la gère automatiquement.
    /// </summary>
    public enum StatType
    {
        // Défensif
        MaxHealth,
        HealthRegen,
        Armor,          // Réduction dégâts physiques
        MagicResist,    // Réduction dégâts magiques

        // Ressource
        MaxMana,
        ManaRegen,

        // Offensif
        AttackDamage,
        AbilityPower,
        AttackSpeed,
        CritChance,
        AttackRange,

        // Pénétration
        ArmorPen,
        MagicPen,

        // Mobilité & utilitaire
        MoveSpeed,
        CooldownReduction, // 0..1 (40% = 0.4)
        Tenacity,          // 0..1, réduit la durée des CC
        Lifesteal,
        Omnivamp
    }
}
