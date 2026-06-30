using Twisted3v3.Core;

namespace Twisted3v3.Combat
{
    /// <summary>
    /// Décrit une instance de dégâts. Passé en <c>in</c> (readonly ref) pour éviter
    /// les allocations. La résolution (armure, pénétration) se fait dans le HealthSystem.
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly DamageType Type;
        public readonly object Source;      // Le champion/objet à l'origine
        public readonly bool IsCrit;
        public readonly bool CanLifesteal;

        public DamageInfo(float amount, DamageType type, object source,
                          bool isCrit = false, bool canLifesteal = false)
        {
            Amount = amount;
            Type = type;
            Source = source;
            IsCrit = isCrit;
            CanLifesteal = canLifesteal;
        }
    }
}
