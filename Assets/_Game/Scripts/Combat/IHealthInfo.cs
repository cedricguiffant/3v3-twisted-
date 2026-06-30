namespace Twisted3v3.Combat
{
    /// <summary>
    /// Expose l'état de vie d'une entité pour l'UI (barres flottantes, HUD), sans
    /// coupler l'UI à l'implémentation (Champion, monstre de jungle, mannequin).
    /// </summary>
    public interface IHealthInfo
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsDead { get; }
    }
}
